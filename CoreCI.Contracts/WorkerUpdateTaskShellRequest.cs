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
using CoreCI.Models;
using System.Collections.Generic;

namespace CoreCI.Contracts
{
    [RouteAttribute("/worker/task/update/shell", "POST")]
    public class WorkerUpdateTaskShellRequest : IReturn<WorkerUpdateTaskShellResponse>
    {
        public Guid WorkerId { get; set; }

        public Guid TaskId { get; set; }

        public List<ShellLine> Lines { get; set; }

        public WorkerUpdateTaskShellRequest(Guid workerId, Guid taskId)
        {
            this.WorkerId = workerId;
            this.TaskId = taskId;
        }
    }

    public class WorkerUpdateTaskShellResponse
    {
    }
}
