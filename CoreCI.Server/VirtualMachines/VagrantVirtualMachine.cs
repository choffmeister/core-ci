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
using System.Threading.Tasks;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;

namespace CoreCI.Server.VirtualMachines
{
    /// <summary>
    /// Implementation of a virtual machine using Vagrant.
    /// </summary>
    public class VagrantVirtualMachine : IVirtualMachine
    {
        private static readonly string _vagrantExecutable = "/usr/bin/vagrant";
        private readonly object _lock = new object();
        private readonly Guid _id;
        private readonly string _folder;
        private readonly string _name;
        private readonly Uri _imageUri;
        private readonly int _cpuCount;
        private readonly int _memorySize;
        private bool _isUp;

        public VagrantVirtualMachine(string name, Uri imageUri, int cpuCount, int memorySize)
        {
            string baseFolder = ConfigurationManager.AppSettings ["virtualMachinesPath"];

            _id = Guid.NewGuid();
            _folder = Path.Combine(baseFolder, _id.ToString());

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

                EnsureDirectoryExists(_folder);
                string vagrantFilePath = Path.Combine(_folder, "Vagrantfile");
                string vagrantFileContent = string.Format(GetVagrantfileTemplate(), _name, _imageUri, _cpuCount, _memorySize);
                File.WriteAllText(vagrantFilePath, vagrantFileContent);

                if (this.ExecuteVagrantCommand("up") != 0)
                {
                    throw new VirtualMachineException("VM could not be started");
                }
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

                if (this.ExecuteVagrantCommand("destroy -f") != 0)
                {
                    throw new VirtualMachineException("VM could ne be destroyed");
                }

                EnsureDirectoryNotExists(_folder);
            }
        }

        public Task<int> Execute(string command)
        {
            return Task<int>.Factory.StartNew(() =>
            {
                string vagrantCommand = string.Format(@"ssh -c ""{0}""", command.Replace("\"", "\\\""));

                return this.ExecuteVagrantCommand(vagrantCommand);
            });
        }

        private int ExecuteVagrantCommand(string vagrantCommand)
        {
            ProcessStartInfo psi = new ProcessStartInfo(_vagrantExecutable, vagrantCommand);
            psi.WorkingDirectory = _folder;
            psi.UseShellExecute = false;

            Process p = Process.Start(psi);
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

        private static string GetVagrantfileTemplate()
        {
            Assembly assembly = typeof(MainClass).Assembly;

            using (Stream stream = assembly.GetManifestResourceStream("CoreCI.Server.Resources.Vagrantfile-template.txt"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
