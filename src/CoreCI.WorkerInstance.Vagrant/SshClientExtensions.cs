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
using System.Text;

namespace CoreCI.WorkerInstance.Vagrant
{
    public static class SshClientExtensions
    {
        public static int Execute(this SshClient client, string commandText, IShellOutput shellOutput, TimeSpan timeout)
        {
            shellOutput = shellOutput ?? new NullShellOutput();

            using (SshCommand cmd = client.CreateCommand(commandText))
            {
                cmd.CommandTimeout = timeout;
                DateTime startTime = DateTime.UtcNow;
                IAsyncResult asynch = cmd.BeginExecute();
                Encoding encoding = client.ConnectionInfo.Encoding;

                while (!asynch.IsCompleted)
                {
                    if (DateTime.UtcNow.Subtract(startTime) > cmd.CommandTimeout)
                    {
                        throw new SshOperationTimeoutException();
                    }

                    bool idle = false;

                    idle |= ConsumeStream(cmd.OutputStream, shellOutput.WriteStandardOutput, encoding);
                    idle |= ConsumeStream(cmd.ExtendedOutputStream, shellOutput.WriteStandardError, encoding);

                    if (idle)
                    {
                        Thread.Sleep(10);
                    }
                }

                ConsumeStream(cmd.OutputStream, shellOutput.WriteStandardOutput, encoding);
                ConsumeStream(cmd.ExtendedOutputStream, shellOutput.WriteStandardError, encoding);

                cmd.EndExecute(asynch);

                return cmd.ExitStatus;
            }
        }

        private static bool ConsumeStream(Stream stream, Action<string> action, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;

            if (stream != null && stream.Length > 0L)
            {
                using (StreamReader reader = new StreamReader(stream, encoding))
                {
                    action(reader.ReadToEnd());

                    return true;
                }
            }

            return false;
        }
    }
}
