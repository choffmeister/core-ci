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
using System.Configuration;
using System.Threading.Tasks;
using System.Threading;
using CoreCI.Worker.VirtualMachines;
using System.Reflection;
using System.IO;
using Renci.SshNet;
using Renci.SshNet.Common;
using ServiceStack.ServiceClient.Web;
using CoreCI.Contracts;
using CoreCI.Common;
using ServiceStack.Text;
using CoreCI.Models;

namespace CoreCI.Worker
{
    /// <summary>
    /// Worker executable.
    /// </summary>
    public class MainClass
    {
        private static readonly Guid _workerId = Guid.Parse(ConfigurationManager.AppSettings ["workerId"]);
        private static readonly string _coordinatorBaseAddress = ConfigurationManager.AppSettings ["coordinatorApiBaseAddress"];

        /// <summary>
        /// Main entry point.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        public static void Main(string[] args)
        {
            TaskLoop keepAliveLoop = new TaskLoop(KeepAliveLoop, 30000);
            TaskLoop doWorkLoop = new TaskLoop(DoWorkLoop, 1000);

            try
            {
                keepAliveLoop.Start();
                doWorkLoop.Start();

                UnixHelper.WaitForSignal();
            }
            finally
            {
                doWorkLoop.Stop();
                keepAliveLoop.Stop();
            }
        }

        private static bool DoWorkLoop()
        {
            try
            {
                using (JsonServiceClient client = new JsonServiceClient(_coordinatorBaseAddress))
                {
                    WorkerGetTaskResponse resp = client.Post(new WorkerGetTaskRequest(_workerId));

                    if (resp.Task != null)
                    {
                        using (var vm = new VagrantVirtualMachine("precise64", new Uri("http://files.vagrantup.com/precise64.box"), 2, 1024))
                        {
                            vm.Up();

                            using (SshClient vmShell = vm.CreateClient())
                            {
                                vmShell.Connect();
                                vmShell.Execute(resp.Task.Script, Console.Out);
                                vmShell.Disconnect();
                            }

                            vm.Down();
                        }

                        return true;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);

                return false;
            }
        }

        private static bool KeepAliveLoop()
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
                Console.Error.WriteLine(ex.Message);

                return false;
            }
        }
    }
}
