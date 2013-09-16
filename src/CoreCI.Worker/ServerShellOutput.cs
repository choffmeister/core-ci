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
using ServiceStack.ServiceClient.Web;
using System.Collections.Generic;
using CoreCI.Common.Shell;
using CoreCI.Contracts;

namespace CoreCI.Worker
{
    public class ServerShellOutput : IShellOutput
    {
        private readonly JsonServiceClient _client;
        private readonly Guid _workerId;
        private readonly Guid _taskId;
        private int _index;

        public ServerShellOutput(JsonServiceClient client, Guid workerId, Guid taskId)
        {
            _client = client;
            _workerId = workerId;
            _taskId = taskId;
            _index = 0;
        }

        public void Dispose()
        {
        }

        public void WriteStandardInput(string s)
        {
            this.Write(s, ShellLineType.StandardInput);
        }

        public void WriteStandardOutput(string s)
        {
            this.Write(s, ShellLineType.StandardOutput);
        }

        public void WriteStandardError(string s)
        {
            this.Write(s, ShellLineType.StandardError);
        }

        private void Write(string s, ShellLineType type)
        {
            // TODO: throttle and group multiple lines into one request
            _client.Post(new DispatcherTaskUpdateShellRequest(_workerId, _taskId)
            {
                Lines = new List<ShellLine>() {
                    new ShellLine()
                    {
                        Index = _index++,
                        Content = s,
                        Type = type
                    }
                }
            });
        }
    }
}
