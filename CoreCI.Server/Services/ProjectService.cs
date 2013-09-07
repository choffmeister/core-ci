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
using ServiceStack.ServiceInterface;
using CoreCI.Models;
using CoreCI.Contracts;
using ServiceStack.Common.Web;

namespace CoreCI.Server.Services
{
    public class TaskService : Service
    {
        private readonly ITaskRepository _taskRepository;
        private readonly ITaskShellRepository _taskShellRepository;

        public TaskService(ITaskRepository taskRepository, ITaskShellRepository taskShellRepository)
        {
            _taskRepository = taskRepository;
            _taskShellRepository = taskShellRepository;
        }

        public override void Dispose()
        {
            _taskRepository.Dispose();
            _taskShellRepository.Dispose();
        }

        public TaskListResponse Get(TaskListRequest req)
        {
            return new TaskListResponse()
            {
                Tasks = _taskRepository.OrderByDescending(t => t.CreatedAt).ToList()
            };
        }

        public TaskRetrieveResponse Get(TaskRetrieveRequest req)
        {
            TaskEntity task = _taskRepository.GetEntityById(req.TaskId);
            TaskShellEntity taskShell = _taskShellRepository.SingleOrDefault(ts => ts.TaskId == task.Id);

            return new TaskRetrieveResponse()
            {
                Task = task,
                Shell = taskShell
            };
        }
    }
}
