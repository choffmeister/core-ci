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
using System.Linq;
using System.IO;
using ServiceStack.ServiceClient.Web;
using System.Collections.Generic;
using CoreCI.Common.Shell;
using CoreCI.Contracts;
using NLog.Targets.Wrappers;
using Renci.SshNet;
using System.Text;

namespace CoreCI.Worker
{
    public class ServerShellOutput : BufferedShellOutput
    {
        private readonly JsonServiceClient _client;
        private readonly Guid _workerId;
        private readonly Guid _taskId;
        private readonly int _index;

        public ServerShellOutput(JsonServiceClient client, Guid workerId, Guid taskId, int index)
        {
            _client = client;
            _workerId = workerId;
            _taskId = taskId;
            _index = index;
        }

        public override void WriteStandardOutput(string s)
        {
            base.WriteStandardOutput(s);

            this.Update();
        }

        public override void WriteStandardError(string s)
        {
            base.WriteStandardError(s);

            this.Update();
        }

        private void Update()
        {
            _client.Post(new DispatcherTaskUpdateShellRequest(_workerId, _taskId)
            {
                Index = _index,
                Output = this.Text
            });
        }
    }
}
