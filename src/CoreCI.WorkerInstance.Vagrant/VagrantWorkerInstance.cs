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
using CoreCI.Common;
using CoreCI.Common.Shell;
using NLog;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace CoreCI.WorkerInstance.Vagrant
{
    public class VagrantWorkerInstance : IWorkerInstance
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly string vagrantExecutablePath;
        private readonly string vagrantVirtualMachinesPath;
        private readonly string machine;
        private readonly IVirtualMachine vm;
        private SshClient shell;

        public VagrantWorkerInstance(string vagrantExecutablePath, string vagrantVirtualMachinesPath, string machine, string boxUrls)
        {
            this.vagrantExecutablePath = vagrantExecutablePath;
            this.vagrantVirtualMachinesPath = vagrantVirtualMachinesPath;
            this.machine = machine;

            Uri machineUri = new Uri(boxUrls + machine + ".box");
            this.vm = new VagrantVirtualMachine(this.vagrantExecutablePath, this.vagrantVirtualMachinesPath, this.machine, machineUri, 2, 1024);
        }

        public void Dispose()
        {
            this.shell.Dispose();
            this.vm.Dispose();
        }

        public void Up()
        {
            Log.Info("Bringing VM {0} up", this.machine);

            this.vm.Up();
            this.shell = this.vm.CreateClient();
            this.shell.Connect();
        }

        public void Down()
        {
            this.shell.Disconnect();
            this.vm.Down();

            Log.Info("Brought VM {0} down", this.machine);
        }

        public void Execute(string commandLine, IShellOutput shellOutput = null)
        {
            try
            {
                int exitCode = this.shell.Execute(commandLine, shellOutput, TimeSpan.FromMinutes(30));

                if (exitCode != 0)
                {
                    throw new ShellCommandFailedException(exitCode);
                }
            }
            catch (SshOperationTimeoutException ex)
            {
                throw new ShellCommandFailedException(1, "Command timed out", ex);
            }
        }
    }
}
