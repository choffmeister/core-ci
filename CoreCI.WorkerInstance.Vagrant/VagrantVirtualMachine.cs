/*
 * Copyright (C) 2013 Christian Hoffmeister
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see {http://www.gnu.org/licenses/}.
 */
using System;
using System.IO;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Renci.SshNet;
using NLog;
using CoreCI.Common;
using System.Security.Cryptography;

namespace CoreCI.WorkerInstance.Vagrant
{
    /// <summary>
    /// Implementation of a virtual machine using Vagrant.
    /// </summary>
    public class VagrantVirtualMachine : IVirtualMachine
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static readonly object _upLock = new object();
        private readonly object _lock = new object();
        private readonly string _vagrantExecutable;
        private readonly string _vagrantVirtualMachinesPath;
        private readonly Guid _id;
        private readonly string _folder;
        private readonly string _name;
        private readonly Uri _imageUri;
        private readonly int _cpuCount;
        private readonly int _memorySize;
        private bool _isUp;
        private string _publicKey;
        private string _privateKey;
        private ConnectionInfo _connectionInfo;

        public ConnectionInfo ConnectionInfo
        {
            get
            {
                if (!_isUp)
                {
                    throw new InvalidOperationException();
                }

                return _connectionInfo;
            }
        }

        public VagrantVirtualMachine(string vagrantExecutablePath, string vagrantVirtualMachinesPath, string name, Uri imageUri, int cpuCount, int memorySize)
        {
            _vagrantExecutable = vagrantExecutablePath;
            _vagrantVirtualMachinesPath = vagrantVirtualMachinesPath;

            _id = Guid.NewGuid();
            _folder = Path.Combine(_vagrantVirtualMachinesPath, _id.ToString());

            _name = name;
            _imageUri = imageUri;
            _cpuCount = cpuCount;
            _memorySize = memorySize;
            _isUp = false;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_isUp)
                {
                    this.Down();
                }
            }
        }

        public void Up()
        {
            lock (_lock)
            {
                if (_isUp)
                {
                    throw new InvalidOperationException();
                }
                _isUp = true;

                _logger.Trace("Generating new RSA key");
                RSA rsa = new RSACryptoServiceProvider(768);
                _publicKey = rsa.ToOpenSshPublicKeyFileString(string.Format("{0}@core-ci", _id));
                _privateKey = rsa.ToOpenSshPrivateKeyFileString();

                EnsureDirectoryExists(_folder);
                string vagrantFilePath = Path.Combine(_folder, "Vagrantfile");
                string vagrantFileContent = GetResource("Vagrantfile-template.txt", _name, _imageUri, _cpuCount, _memorySize);
                File.WriteAllText(vagrantFilePath, vagrantFileContent);

                string vagrantFileBootstrapPath = Path.Combine(_folder, "Vagrantfile-bootstrap.sh");
                string vagrantFileBootstrapContent = GetResource("Vagrantfile-bootstrap-template.txt", _publicKey);
                File.WriteAllText(vagrantFileBootstrapPath, vagrantFileBootstrapContent);

                // ensure that no two machines are upped in parallel
                lock (_upLock)
                {
                    this.VagrantUp();
                }

                this.VagrantProvision();
                _connectionInfo = this.VagrantSshConfig();
            }
        }

        public void Down()
        {
            lock (_lock)
            {
                if (!_isUp)
                {
                    throw new InvalidOperationException();
                }
                _isUp = false;

                _connectionInfo = null;
                this.VagrantDown();
                EnsureDirectoryNotExists(_folder);
            }
        }

        public SshClient CreateClient()
        {
            lock (_lock)
            {
                if (!_isUp)
                {
                    throw new InvalidOperationException();
                }

                return new SshClient(_connectionInfo);
            }
        }

        private void VagrantUp()
        {
            _logger.Info("Starting VM");
            ProcessResult result = this.ExecuteVagrantCommand("up --no-provision");

            if (!result.Success)
            {
                throw new VagrantException("VM could not be started\n\n" + result.Output);
            }
        }

        private void VagrantDown()
        {
            this.ExecuteVagrantCommand("destroy -f");
            _logger.Info("Destroyed VM");
        }

        private void VagrantProvision()
        {
            ProcessResult result = this.ExecuteVagrantCommand("provision");

            if (!result.Success)
            {
                throw new VagrantException("VM could not be provisioned\n\n" + result.Output);
            }
        }

        private ConnectionInfo VagrantSshConfig()
        {
            ProcessResult result = this.ExecuteVagrantCommand("ssh-config");

            if (!result.Success)
            {
                throw new VagrantException("Could not determine SSH connection information\n\n" + result.Output);
            }

            string sshConfig = result.StdOut;
            Match hostNameMatch = Regex.Match(sshConfig, @"HostName\s(?<HostName>[^\s]+)");
            Match portMatch = Regex.Match(sshConfig, @"Port\s(?<Port>[\d]+)");

            if (!hostNameMatch.Success || !portMatch.Success)
            {
                throw new VagrantException("Could not determine SSH connection information\n\n" + result.Output);
            }

            string hostName = hostNameMatch.Groups ["HostName"].Value;
            int port = int.Parse(portMatch.Groups ["Port"].Value);
            string userName = "coreci";
            PrivateKeyFile privateKey = new PrivateKeyFile(new MemoryStream(Encoding.ASCII.GetBytes(_privateKey)));

            return new ConnectionInfo(hostName, port, userName, new PrivateKeyAuthenticationMethod(userName, privateKey));
        }

        private ProcessResult ExecuteVagrantCommand(string vagrantCommand)
        {
            return ProcessHelper.Execute(_vagrantExecutable, vagrantCommand, _folder);
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static void EnsureDirectoryNotExists(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        private static string GetResource(string name, params object[] args)
        {
            using (Stream stream = new FileStream(Path.Combine("Resources", name), FileMode.Open, FileAccess.Read))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string resource = reader.ReadToEnd();

                    return string.Format(resource, args);
                }
            }
        }
    }
}
