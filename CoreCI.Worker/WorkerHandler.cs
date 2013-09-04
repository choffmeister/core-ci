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
        private readonly string _vagrantExecutablePath;
        private readonly string _vagrantVirtualMachinesPath;
        private readonly Guid _workerId;
        private readonly string _serverApiBaseAddress;
        private readonly TaskLoop _keepAliveLoop;
        private readonly TaskLoop _doWorkLoop;

        public WorkerHandler(IConfigurationProvider configurationProvider)
        {
            _configurationProvider = configurationProvider;

            _vagrantExecutablePath = _configurationProvider.GetSettingString("workerVagrantExecutablePath");
            _vagrantVirtualMachinesPath = _configurationProvider.GetSettingString("workerVagrantVirtualMachinesPath");
            _workerId = Guid.Parse(_configurationProvider.GetSettingString("workerId"));
            _serverApiBaseAddress = _configurationProvider.GetSettingString("workerServerApiBaseAddress");

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
                using (JsonServiceClient client = new JsonServiceClient(_serverApiBaseAddress))
                {
                    DispatcherTaskPollResponse resp = client.Post(new DispatcherTaskPollRequest(_workerId));

                    if (resp.Task != null)
                    {
                        TaskEntity task = resp.Task;
                        TaskConfiguration config = task.Configuration;
                        int index = 1;

                        using (var vm = new VagrantVirtualMachine(_vagrantExecutablePath, _vagrantVirtualMachinesPath, config.Machine, new Uri("http://boxes.choffmeister.de/" + config.Machine + ".box"), 2, 1024))
                        {
                            _logger.Info("Bringing VM {0} up for task {1}", config.Machine, task.Id);
                            this.UpdateShell(client, task.Id, "Starting VM " + config.Machine + "...", ref index);

                            vm.Up();

                            using (SshClient vmShell = vm.CreateClient())
                            {
                                try
                                {
                                    this.UpdateStarted(client, task.Id);

                                    vmShell.Connect();

                                    foreach (string commandLine in SshClientHelper.SplitIntoCommandLines(config.Script))
                                    {
                                        _logger.Trace("Executing command '{0}' for task {1}", commandLine, task.Id);
                                        vmShell.Execute(commandLine, line => this.UpdateShell(client, task.Id, line.Content, ref index, line.Type));
                                    }

                                    vmShell.Disconnect();

                                    this.UpdateShell(client, task.Id, "Exited with code 0", ref index);
                                    this.UpdateFinished(client, task.Id, 0);
                                }
                                catch (SshCommandFailedException ex)
                                {
                                    this.UpdateShell(client, task.Id, "Exited with code " + ex.ExitCode.ToString(), ref index);
                                    this.UpdateFinished(client, task.Id, ex.ExitCode);
                                }
                            }

                            vm.Down();

                            _logger.Info("Brought VM {0} from task {1} down", config.Machine, task.Id);
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

        private void UpdateStarted(JsonServiceClient client, Guid taskId)
        {
            client.Post(new DispatcherTaskUpdateStartRequest(taskId));
        }

        private void UpdateFinished(JsonServiceClient client, Guid taskId, int exitCode)
        {
            client.Post(new DispatcherTaskUpdateFinishRequest(taskId, exitCode));
        }

        private void UpdateShell(JsonServiceClient client, Guid taskId, string content, ref int index, ShellLineType type = ShellLineType.StandardOutput)
        {
            // TODO: throttle and group multiple lines into one request
            client.Post(new DispatcherTaskUpdateShellRequest(_workerId, taskId)
            {
                Lines = new List<ShellLine>() {
                    new ShellLine()
                    {
                        Index = index++,
                        Content = content,
                        Type = type
                    }
                }
            });
        }

        private bool KeepAliveLoop()
        {
            try
            {
                using (JsonServiceClient client = new JsonServiceClient(_serverApiBaseAddress))
                {
                    client.Post(new DispatcherWorkerKeepAliveRequest(_workerId));

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
