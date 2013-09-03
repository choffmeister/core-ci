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

namespace CoreCI.Worker.VirtualMachines
{
    /// <summary>
    /// Implementation of a virtual machine using Vagrant.
    /// </summary>
    public class VagrantVirtualMachine : IVirtualMachine
    {
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

                string publicKey = GetResource("id_rsa.pub");

                EnsureDirectoryExists(_folder);
                string vagrantFilePath = Path.Combine(_folder, "Vagrantfile");
                string vagrantFileContent = GetResource("Vagrantfile-template.txt", _name, _imageUri, _cpuCount, _memorySize);
                File.WriteAllText(vagrantFilePath, vagrantFileContent);

                string vagrantFileBootstrapPath = Path.Combine(_folder, "Vagrantfile-bootstrap.sh");
                string vagrantFileBootstrapContent = GetResource("Vagrantfile-bootstrap-template.txt", publicKey);
                File.WriteAllText(vagrantFileBootstrapPath, vagrantFileBootstrapContent);

                StringWriter stdOutWriter = new StringWriter();
                StringWriter stdErrorWriter = new StringWriter();
                if (this.ExecuteVagrantCommand("up", stdOutWriter, stdErrorWriter) != 0)
                {
                    throw new VirtualMachineException("VM could not be started\n\n" + stdErrorWriter.ToString());
                }

                StringBuilder sb = new StringBuilder();
                TextWriter writer = new StringWriter(sb);

                if (this.ExecuteVagrantCommand("ssh-config", writer) != 0)
                {
                    throw new VirtualMachineException("Could not determine SSH connection information");
                }

                string sshConfig = sb.ToString();
                Match hostNameMatch = Regex.Match(sshConfig, @"HostName\s(?<HostName>[^\s]+)");
                Match portMatch = Regex.Match(sshConfig, @"Port\s(?<Port>[\d]+)");

                if (!hostNameMatch.Success || !portMatch.Success)
                {
                    throw new VirtualMachineException("Could not determine SSH connection information");
                }

                string hostName = hostNameMatch.Groups ["HostName"].Value;
                int port = int.Parse(portMatch.Groups ["Port"].Value);
                string userName = "coreci";
                PrivateKeyFile privateKey = new PrivateKeyFile("Resources/id_rsa");

                _connectionInfo = new ConnectionInfo(hostName, port, userName, new PrivateKeyAuthenticationMethod(userName, privateKey));
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
                this.ExecuteVagrantCommand("destroy -f");
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

        private int ExecuteVagrantCommand(string vagrantCommand, TextWriter stdOutWriter = null, TextWriter stdErrorWriter = null)
        {
            ProcessStartInfo psi = new ProcessStartInfo(_vagrantExecutable, vagrantCommand);
            psi.WorkingDirectory = _folder;
            psi.UseShellExecute = false;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            Process p = Process.Start(psi);

            p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (stdOutWriter != null)
                {
                    stdOutWriter.Write(e.Data);
                }
            };
            p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (stdErrorWriter != null)
                {
                    stdErrorWriter.Write(e.Data);
                }
            };
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();

            return p.ExitCode;
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
            Assembly assembly = typeof(WorkerExecutable).Assembly;

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
