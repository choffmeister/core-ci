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
using System.Threading;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace CoreCI.Worker
{
    public static class SshClientExtensions
    {
        public static int Execute(this SshClient client, string commandText, TextWriter stdOut)
        {
            using (SshCommand cmd = client.CreateCommand(commandText))
            {
                cmd.CommandTimeout = TimeSpan.FromSeconds(60.0);

                DateTime startTime = DateTime.UtcNow;
                IAsyncResult asynch = cmd.BeginExecute();
                StreamReader reader = new StreamReader(cmd.OutputStream);

                while (!asynch.IsCompleted)
                {
                    if (DateTime.UtcNow.Subtract(startTime) > cmd.CommandTimeout)
                    {
                        throw new SshOperationTimeoutException();
                    }
                    else if (cmd.OutputStream.Length > 0)
                    {
                        stdOut.Write(reader.ReadToEnd());
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }

                if (cmd.OutputStream.Length > 0)
                {
                    stdOut.Write(reader.ReadToEnd());
                }

                cmd.EndExecute(asynch);
                return cmd.ExitStatus;
            }
        }
    }
}
