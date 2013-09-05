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
using CoreCI.Common.Shell;
using System.IO;
using Renci.SshNet.Common;
using System.Threading;

namespace CoreCI.WorkerInstance.Vagrant
{
    public static class SshClientExtensions
    {
        public static int Execute(this SshClient client, string commandText, IShellOutput shellOutput, TimeSpan timeout)
        {
            using (SshCommand cmd = client.CreateCommand(commandText))
            {
                shellOutput.WriteStandardInput(commandText);

                cmd.CommandTimeout = timeout;
                DateTime startTime = DateTime.UtcNow;
                IAsyncResult asynch = cmd.BeginExecute();
                StreamReader stdOutReader = new StreamReader(cmd.OutputStream);
                StreamReader stdErrReader = new StreamReader(cmd.ExtendedOutputStream);

                while (!asynch.IsCompleted)
                {
                    if (DateTime.UtcNow.Subtract(startTime) > cmd.CommandTimeout)
                    {
                        throw new SshOperationTimeoutException();
                    }
                    else if (cmd.OutputStream.Length > 0)
                    {
                        shellOutput.WriteStandardOutput(stdOutReader.ReadLine());
                    }
                    else if (cmd.ExtendedOutputStream.Length > 0)
                    {
                        shellOutput.WriteStandardError(stdErrReader.ReadLine());
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }

                while (cmd.OutputStream.Length > 0)
                {
                    shellOutput.WriteStandardOutput(stdOutReader.ReadLine());
                }

                while (cmd.ExtendedOutputStream.Length > 0)
                {
                    shellOutput.WriteStandardError(stdErrReader.ReadLine());
                }

                cmd.EndExecute(asynch);

                return cmd.ExitStatus;
            }
        }
    }
}
