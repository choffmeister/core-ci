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
using Renci.SshNet;
using NLog;
using CoreCI.Common.Shell;
using CoreCI.Common;

namespace CoreCI.WorkerInstance.Vagrant
{
    public class VagrantWorkerInstance : IWorkerInstance
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly string _vagrantExecutablePath;
        private readonly string _vagrantVirtualMachinesPath;
        private readonly string _machine;
        private readonly IVirtualMachine _vm;
        private SshClient _shell;

        public IShellOutput ShellOutput { get; set; }

        public VagrantWorkerInstance(string vagrantExecutablePath, string vagrantVirtualMachinesPath, string machine)
        {
            _vagrantExecutablePath = vagrantExecutablePath;
            _vagrantVirtualMachinesPath = vagrantVirtualMachinesPath;
            _machine = machine;

            Uri machineUri = new Uri("http://boxes.choffmeister.de/" + machine + ".box");
            _vm = new VagrantVirtualMachine(_vagrantExecutablePath, _vagrantVirtualMachinesPath, machine, machineUri, 2, 1024);
        }

        public void Dispose()
        {
            _shell.Dispose();
            _vm.Dispose();
        }

        public void Up()
        {
            _logger.Info("Bringing VM {0} up", _machine);
            this.ShellOutput.WriteStandardInput("Starting VM {0}...", _machine);

            _vm.Up();
            _shell = _vm.CreateClient();
            _shell.Connect();
        }

        public void Down()
        {
            _shell.Disconnect();
            _vm.Down();

            _logger.Info("Brought VM {0} down", _machine);
        }

        public void Execute(string commandLine)
        {
            _shell.Execute(commandLine, this.ShellOutput);
        }
    }
}
