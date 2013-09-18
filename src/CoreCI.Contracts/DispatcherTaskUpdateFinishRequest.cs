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
using ServiceStack.ServiceHost;

namespace CoreCI.Contracts
{
    [RouteAttribute("/dispatcher/task/update/finish", "POST")]
    public class DispatcherTaskUpdateFinishRequest : IReturn<DispatcherTaskUpdateFinishResponse>
    {
        public Guid TaskId { get; set; }

        public int ExitCode { get; set; }

        public DispatcherTaskUpdateFinishRequest(Guid taskId, int exitCode)
        {
            this.TaskId = taskId;
            this.ExitCode = exitCode;
        }
    }

    public class DispatcherTaskUpdateFinishResponse
    {
    }
}
