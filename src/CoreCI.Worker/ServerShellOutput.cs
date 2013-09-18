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
using CoreCI.Common.Shell;
using CoreCI.Contracts;
using ServiceStack.ServiceClient.Web;

namespace CoreCI.Worker
{
    public class ServerShellOutput : BufferedShellOutput
    {
        private readonly JsonServiceClient client;
        private readonly Guid workerId;
        private readonly Guid taskId;
        private readonly int index;

        public ServerShellOutput(JsonServiceClient client, Guid workerId, Guid taskId, int index)
        {
            this.client = client;
            this.workerId = workerId;
            this.taskId = taskId;
            this.index = index;
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
            this.client.Post(new DispatcherTaskUpdateShellRequest(this.workerId, this.taskId)
            {
                Index = this.index,
                Output = this.Text
            });
        }
    }
}
