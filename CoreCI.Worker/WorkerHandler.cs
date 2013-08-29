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
using NLog;
using ServiceStack.ServiceClient.Web;
using CoreCI.Contracts;
using CoreCI.Models;
using CoreCI.Worker.VirtualMachines;
using Renci.SshNet;
using System.Collections.Generic;

namespace CoreCI.Worker
{
    public class WorkerHandler
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IConfigurationProvider _configurationProvider;
        private readonly Guid _workerId;
        private readonly string _coordinatorBaseAddress;
        private readonly TaskLoop _keepAliveLoop;
        private readonly TaskLoop _doWorkLoop;

        public WorkerHandler(IConfigurationProvider configurationProvider)
        {
            _configurationProvider = configurationProvider;

            _workerId = Guid.Parse(_configurationProvider.GetSettingString("workerId"));
            _coordinatorBaseAddress = _configurationProvider.GetSettingString("coordinatorApiBaseAddress");

            _keepAliveLoop = new TaskLoop(this.KeepAliveLoop, 1000);
            _doWorkLoop = new TaskLoop(this.DoWorkLoop, 1000);
        }

        public void Start()
        {
            _logger.Info("Start working");
            _keepAliveLoop.Start();
            _doWorkLoop.Start();
            _logger.Info("Started");
        }

        public void Stop()
        {
            _logger.Info("Stop working");
            _doWorkLoop.Stop();
            _keepAliveLoop.Stop();
            _logger.Info("Stopped");
        }

        private bool DoWorkLoop()
        {
            try
            {
                using (JsonServiceClient client = new JsonServiceClient(_coordinatorBaseAddress))
                {
                    WorkerGetTaskResponse resp = client.Post(new WorkerGetTaskRequest(_workerId));

                    if (resp.Task != null)
                    {
                        TaskEntity task = resp.Task;

                        using (var vm = new VagrantVirtualMachine("precise64", new Uri("http://files.vagrantup.com/precise64.box"), 2, 1024))
                        {
                            _logger.Info("Bringing VM {0} up for task {1}", "precise64", task.Id);

                            vm.Up();

                            using (SshClient vmShell = vm.CreateClient())
                            {
                                try
                                {
                                    int index = 0;
                                    vmShell.Connect();

                                    foreach (string commandLine in SshClientHelper.SplitIntoCommandLines(task.Script))
                                    {
                                        _logger.Trace("Executing command '{0}' for task {1}", commandLine, task.Id);

                                        vmShell.Execute(commandLine, ref index, line => {
                                            // TODO: throttle and group multiple lines into one request
                                            client.Post(new WorkerUpdateTaskShellRequest(_workerId, task.Id)
                                            {
                                                Lines = new List<ShellLine>() { line }
                                            });
                                        });
                                    }

                                    vmShell.Disconnect();

                                    client.Post(new WorkerUpdateTaskRequest(_workerId, task.Id, 0));
                                }
                                catch (SshCommandFailedException ex)
                                {
                                    client.Post(new WorkerUpdateTaskRequest(_workerId, task.Id, ex.ExitCode));
                                }
                            }

                            vm.Down();

                            _logger.Info("Brought VM {0} from task {1} down", "precise64", task.Id);
                        }

                        return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);

                return false;
            }
        }

        private bool KeepAliveLoop()
        {
            try
            {
                using (JsonServiceClient client = new JsonServiceClient(_coordinatorBaseAddress))
                {
                    client.Post(new WorkerKeepAliveRequest(_workerId));

                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);

                return false;
            }
        }
    }
}
