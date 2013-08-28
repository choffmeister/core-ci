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
            TaskLoop loop = new TaskLoop(KeepAliveTask, 5000);

            try
            {
                loop.Start();

                using (var vm = new VagrantVirtualMachine("precise64", new Uri("http://files.vagrantup.com/precise64.box"), 2, 1024))
                {
                    vm.Up();

                    using (SshClient client = vm.CreateClient())
                    {
                        client.Connect();

                        Execute(client, "id");

                        client.Disconnect();
                    }

                    vm.Down();
                }
            }
            finally
            {
                loop.Stop();
            }
        }

        private static bool KeepAliveTask()
        {
            try
            {
                using (JsonServiceClient client = new JsonServiceClient(_coordinatorBaseAddress))
                {
                    client.Get(new WorkerKeepAliveRequest(_workerId));
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }

            return false;
        }

        public static int Execute(SshClient client, string commandText)
        {
            Console.WriteLine("$ {0}", commandText);

            using (SshCommand cmd = client.CreateCommand(commandText))
            {
                cmd.CommandTimeout = TimeSpan.FromSeconds(60.0);

                DateTime startTime = DateTime.UtcNow;
                IAsyncResult asynch = cmd.BeginExecute();
                StreamReader reader = new StreamReader(cmd.OutputStream);
                StreamReader readerExtended = new StreamReader(cmd.ExtendedOutputStream);

                while (!asynch.IsCompleted)
                {
                    if (DateTime.UtcNow.Subtract(startTime) > cmd.CommandTimeout)
                    {
                        throw new SshOperationTimeoutException();
                    }
                    else if (cmd.OutputStream.Length > 0)
                    {
                        Console.Write(reader.ReadToEnd());
                    }
                    else if (cmd.ExtendedOutputStream.Length > 0)
                    {
                        Console.Error.Write(readerExtended.ReadToEnd());
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }

                if (cmd.OutputStream.Length > 0)
                {
                    Console.Write(reader.ReadToEnd());
                }

                if (cmd.ExtendedOutputStream.Length > 0)
                {
                    Console.Error.Write(readerExtended.ReadToEnd());
                }

                cmd.EndExecute(asynch);
                return cmd.ExitStatus;
            }
        }
    }
}
