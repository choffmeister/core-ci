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
using CoreCI.Models;
using System.Collections.Generic;
using System.Text;

namespace CoreCI.Worker
{
    public static class SshClientHelper
    {
        public static void Execute(this SshClient client, string commandText, Action<ShellLine> callback)
        {
            using (SshCommand cmd = client.CreateCommand(commandText))
            {
                callback(new ShellLine()
                {
                    Index = 0,
                    Type = ShellLineType.StandardInput,
                    Content = commandText
                });

                cmd.CommandTimeout = TimeSpan.FromHours(1.0);
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
                        callback(new ShellLine()
                        {
                            Index = 0,
                            Type = ShellLineType.StandardOutput,
                            Content = stdOutReader.ReadLine()
                        });
                    }
                    else if (cmd.ExtendedOutputStream.Length > 0)
                    {
                        callback(new ShellLine()
                        {
                            Index = 0,
                            Type = ShellLineType.StandardError,
                            Content = stdErrReader.ReadLine()
                        });
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }

                while (cmd.OutputStream.Length > 0)
                {
                    callback(new ShellLine()
                    {
                        Index = 0,
                        Type = ShellLineType.StandardOutput,
                        Content = stdOutReader.ReadLine()
                    });
                }

                while (cmd.ExtendedOutputStream.Length > 0)
                {
                    callback(new ShellLine()
                    {
                        Index = 0,
                        Type = ShellLineType.StandardError,
                        Content = stdErrReader.ReadLine()
                    });
                }

                cmd.EndExecute(asynch);

                if (cmd.ExitStatus != 0)
                {
                    throw new SshCommandFailedException(cmd.ExitStatus);
                }
            }
        }

        public static IEnumerable<string> SplitIntoCommandLines(string script)
        {
            StringBuilder sb = new StringBuilder();
            StringReader reader = new StringReader(script);
            string rawLine = null;

            while ((rawLine = reader.ReadLine()) != null)
            {
                if (rawLine.EndsWith("\\"))
                {
                    sb.Append(rawLine.Substring(0, rawLine.Length - 1));
                }
                else
                {
                    sb.Append(rawLine);
                    string line = sb.ToString();
                    sb.Clear();

                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        yield return line;
                    }
                }
            }
        }
    }
}
