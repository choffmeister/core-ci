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
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CoreCI.Common;
using NLog;
using Renci.SshNet;

namespace CoreCI.WorkerInstance.Vagrant
{
    /// <summary>
    /// Implementation of a virtual machine using Vagrant.
    /// </summary>
    public class VagrantVirtualMachine : IVirtualMachine
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly object VagrantUpLock = new object();
        private readonly object lockObject = new object();
        private readonly string vagrantExecutable;
        private readonly string vagrantVirtualMachinesPath;
        private readonly Guid id;
        private readonly string folder;
        private readonly string name;
        private readonly Uri imageUri;
        private readonly int cpuCount;
        private readonly int memorySize;
        private bool isUp;
        private string publicKey;
        private string privateKey;
        private ConnectionInfo connectionInfo;

        public ConnectionInfo ConnectionInfo
        {
            get
            {
                if (!this.isUp)
                {
                    throw new InvalidOperationException();
                }

                return this.connectionInfo;
            }
        }

        public VagrantVirtualMachine(string vagrantExecutablePath, string vagrantVirtualMachinesPath, string name, Uri imageUri, int cpuCount, int memorySize)
        {
            this.vagrantExecutable = vagrantExecutablePath;
            this.vagrantVirtualMachinesPath = vagrantVirtualMachinesPath;

            this.id = Guid.NewGuid();
            this.folder = Path.Combine(this.vagrantVirtualMachinesPath, this.id.ToString());

            this.name = name;
            this.imageUri = imageUri;
            this.cpuCount = cpuCount;
            this.memorySize = memorySize;
            this.isUp = false;
        }

        public void Dispose()
        {
            lock (this.lockObject)
            {
                if (this.isUp)
                {
                    this.Down();
                }
            }
        }

        public void Up()
        {
            lock (this.lockObject)
            {
                if (this.isUp)
                {
                    throw new InvalidOperationException();
                }

                this.isUp = true;

                Log.Trace("Generating new RSA key");
                RSA rsa = new RSACryptoServiceProvider(768);
                this.publicKey = rsa.ToOpenSshPublicKeyFileString(string.Format("{0}@core-ci", this.id));
                this.privateKey = rsa.ToOpenSshPrivateKeyFileString();

                EnsureDirectoryExists(this.folder);
                string vagrantFilePath = Path.Combine(this.folder, "Vagrantfile");
                string vagrantFileContent = GetResource("Vagrantfile-template.txt", this.name, this.imageUri, this.cpuCount, this.memorySize);
                File.WriteAllText(vagrantFilePath, vagrantFileContent);

                string vagrantFileBootstrapPath = Path.Combine(this.folder, "Vagrantfile-bootstrap.sh");
                string vagrantFileBootstrapContent = GetResource("Vagrantfile-bootstrap-template.txt", this.publicKey);
                File.WriteAllText(vagrantFileBootstrapPath, vagrantFileBootstrapContent);

                // ensure that no two machines are upped in parallel
                lock (VagrantUpLock)
                {
                    this.VagrantUp();
                }

                this.VagrantProvision();
                this.connectionInfo = this.VagrantSshConfig();
            }
        }

        public void Down()
        {
            lock (this.lockObject)
            {
                if (!this.isUp)
                {
                    throw new InvalidOperationException();
                }

                this.isUp = false;

                this.connectionInfo = null;
                this.VagrantDown();
                EnsureDirectoryNotExists(this.folder);
            }
        }

        public SshClient CreateClient()
        {
            lock (this.lockObject)
            {
                if (!this.isUp)
                {
                    throw new InvalidOperationException();
                }

                return new SshClient(this.connectionInfo);
            }
        }

        private void VagrantUp()
        {
            Log.Info("Starting VM");
            ProcessResult result = this.ExecuteVagrantCommand("up --no-provision");

            if (!result.Success)
            {
                throw new VagrantException("VM could not be started\n\n" + result.Output);
            }
        }

        private void VagrantDown()
        {
            this.ExecuteVagrantCommand("destroy -f");
            Log.Info("Destroyed VM");
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

            string hostName = hostNameMatch.Groups["HostName"].Value;
            int port = int.Parse(portMatch.Groups["Port"].Value);
            string userName = "coreci";
            PrivateKeyFile privateKeyFile = new PrivateKeyFile(new MemoryStream(Encoding.ASCII.GetBytes(this.privateKey)));

            return new ConnectionInfo(hostName, port, userName, new PrivateKeyAuthenticationMethod(userName, privateKeyFile));
        }

        private ProcessResult ExecuteVagrantCommand(string vagrantCommand)
        {
            return ProcessHelper.Execute(this.vagrantExecutable, vagrantCommand, this.folder);
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
            string fullName = string.Format("CoreCI.WorkerInstance.Vagrant.Resources.{0}", name);
            Assembly assembly = typeof(VagrantWorkerInstance).Assembly;

            using (Stream stream = assembly.GetManifestResourceStream(fullName))
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
