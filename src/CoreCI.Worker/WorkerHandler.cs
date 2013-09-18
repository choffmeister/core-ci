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
using CoreCI.Contracts;
using CoreCI.Models;
using CoreCI.WorkerInstance.Vagrant;
using NLog;
using ServiceStack.ServiceClient.Web;

namespace CoreCI.Worker
{
    public class WorkerHandler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly IConfigurationProvider configurationProvider;
        private readonly string vagrantExecutablePath;
        private readonly string vagrantVirtualMachinesPath;
        private readonly Guid workerId;
        private readonly string serverApiBaseAddress;
        private readonly TaskLoop keepAliveLoop;
        private readonly ConcurrentTaskLoop<TaskEntity> workLoop;

        public WorkerHandler(IConfigurationProvider configurationProvider)
        {
            this.configurationProvider = configurationProvider;

            this.vagrantExecutablePath = this.configurationProvider.Get("worker.vagrant.executable");
            this.vagrantVirtualMachinesPath = this.configurationProvider.Get("worker.vagrant.machines");
            this.workerId = Guid.Parse(this.configurationProvider.Get("worker.id"));
            this.serverApiBaseAddress = this.configurationProvider.Get("worker.server");

            this.keepAliveLoop = new TaskLoop(this.KeepAlive, 60000);
            this.workLoop = new ConcurrentTaskLoop<TaskEntity>(this.Dispatch, this.Work, 1000, 4);
        }

        public void Start()
        {
            Log.Info("Start working");
            this.workLoop.Start();
            this.keepAliveLoop.Start();
            Log.Info("Started");
        }

        public void Stop()
        {
            Log.Info("Stop working");
            this.keepAliveLoop.Stop();
            this.workLoop.Stop();
            Log.Info("Stopped");
        }

        private TaskEntity Dispatch()
        {
            try
            {
                using (JsonServiceClient client = new JsonServiceClient(this.serverApiBaseAddress))
                {
                    DispatcherTaskPollResponse resp = client.Post(new DispatcherTaskPollRequest(this.workerId));

                    return resp.Task;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);

                return null;
            }
        }

        private void Work(TaskEntity task)
        {
            int index = 0;
            string boxUrls = this.configurationProvider.Get("worker.vagrant.box_urls");

            try
            {
                using (JsonServiceClient client = new JsonServiceClient(this.serverApiBaseAddress))
                using (IWorkerInstance worker = new VagrantWorkerInstance(this.vagrantExecutablePath, this.vagrantVirtualMachinesPath, task.Configuration.Machine, boxUrls))
                {
                    using (IShellOutput shellOutput = new ServerShellOutput(client, this.workerId, task.Id, index++))
                    {
                        shellOutput.WriteStandardOutput("Starting VM...\n");
                    }

                    worker.Up();

                    try
                    {
                        client.Post(new DispatcherTaskUpdateStartRequest(task.Id));

                        foreach (string commandLine in ShellExtensions.SplitIntoCommandLines(task.Configuration.SecretStartupScript))
                        {
                            worker.Execute(commandLine);
                        }

                        foreach (string script in new string[] { task.Configuration.CheckoutScript, task.Configuration.TestScript })
                        {
                            foreach (string commandLine in ShellExtensions.SplitIntoCommandLines(script))
                            {
                                using (IShellOutput shellOutput = new ServerShellOutput(client, this.workerId, task.Id, index++))
                                {
                                    shellOutput.WriteStandardOutput(commandLine + "\n");
                                    Log.Trace("Executing command '{0}' for task {1}", commandLine, task.Id);
                                    worker.Execute(commandLine, shellOutput);
                                }
                            }
                        }

                        using (IShellOutput shellOutput = new ServerShellOutput(client, this.workerId, task.Id, index++))
                        {
                            shellOutput.WriteStandardOutput("Exited with code 0\n");
                            client.Post(new DispatcherTaskUpdateFinishRequest(task.Id, 0));
                        }
                    }
                    catch (ShellCommandFailedException ex)
                    {
                        using (IShellOutput shellOutput = new ServerShellOutput(client, this.workerId, task.Id, index++))
                        {
                            shellOutput.WriteStandardOutput("Exited with code {0}\n", ex.ExitCode);
                            client.Post(new DispatcherTaskUpdateFinishRequest(task.Id, ex.ExitCode));
                        }
                    }

                    worker.Down();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        private bool KeepAlive()
        {
            try
            {
                using (JsonServiceClient client = new JsonServiceClient(this.serverApiBaseAddress))
                {
                    client.Post(new DispatcherWorkerKeepAliveRequest(this.workerId));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            return false;
        }
    }
}
