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
using CoreCI.Worker.Shell;

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
        private readonly ConcurrentTaskLoop<TaskEntity> _workLoop;

        public WorkerHandler(IConfigurationProvider configurationProvider)
        {
            _configurationProvider = configurationProvider;

            _vagrantExecutablePath = _configurationProvider.GetSettingString("workerVagrantExecutablePath");
            _vagrantVirtualMachinesPath = _configurationProvider.GetSettingString("workerVagrantVirtualMachinesPath");
            _workerId = Guid.Parse(_configurationProvider.GetSettingString("workerId"));
            _serverApiBaseAddress = _configurationProvider.GetSettingString("workerServerApiBaseAddress");

            _keepAliveLoop = new TaskLoop(this.KeepAlive, 60000);
            _workLoop = new ConcurrentTaskLoop<TaskEntity>(this.Dispatch, this.Work, 1000, 4);
        }

        public void Start()
        {
            _logger.Info("Start working");
            _workLoop.Start();
            _keepAliveLoop.Start();
            _logger.Info("Started");
        }

        public void Stop()
        {
            _logger.Info("Stop working");
            _keepAliveLoop.Stop();
            _workLoop.Stop();
            _logger.Info("Stopped");
        }

        private TaskEntity Dispatch()
        {
            try
            {
                using (JsonServiceClient client = new JsonServiceClient(_serverApiBaseAddress))
                {
                    DispatcherTaskPollResponse resp = client.Post(new DispatcherTaskPollRequest(_workerId));

                    return resp.Task;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);

                return null;
            }
        }

        private void Work(TaskEntity task)
        {
            try
            {
                using (JsonServiceClient client = new JsonServiceClient(_serverApiBaseAddress))
                using (IShellOutput shellOutput = new ServerShellOutput(client, _workerId, task.Id))
                using (IWorkerInstance worker = new VagrantWorkerInstance(_vagrantExecutablePath, _vagrantVirtualMachinesPath, task.Configuration.Machine))
                {
                    worker.ShellOutput = shellOutput;
                    worker.Up();

                    try
                    {
                        client.Post(new DispatcherTaskUpdateStartRequest(task.Id));

                        foreach (string commandLine in ShellExtensions.SplitIntoCommandLines(task.Configuration.Script))
                        {
                            _logger.Trace("Executing command '{0}' for task {1}", commandLine, task.Id);
                            worker.Execute(commandLine);
                        }

                        shellOutput.WriteStandardOutput("Exited with code 0");
                        client.Post(new DispatcherTaskUpdateFinishRequest(task.Id, 0));
                    }
                    catch (ShellCommandFailedException ex)
                    {
                        shellOutput.WriteStandardOutput("Exited with code {0}", ex.ExitCode);
                        client.Post(new DispatcherTaskUpdateFinishRequest(task.Id, ex.ExitCode));
                    }

                    worker.Down();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        private bool KeepAlive()
        {
            try
            {
                using (JsonServiceClient client = new JsonServiceClient(_serverApiBaseAddress))
                {
                    client.Post(new DispatcherWorkerKeepAliveRequest(_workerId));
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            return false;
        }
    }
}
