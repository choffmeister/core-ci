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
using System.Collections.Generic;
using System.Linq;
using CoreCI.Contracts;
using CoreCI.Models;
using ServiceStack.ServiceInterface;

namespace CoreCI.Server.Services
{
    public class TaskService : Service
    {
        private readonly ITaskRepository taskRepository;
        private readonly ITaskShellRepository taskShellRepository;

        public TaskService(ITaskRepository taskRepository, ITaskShellRepository taskShellRepository)
        {
            this.taskRepository = taskRepository;
            this.taskShellRepository = taskShellRepository;
        }

        public override void Dispose()
        {
            this.taskRepository.Dispose();
            this.taskShellRepository.Dispose();
        }

        public TaskListResponse Get(TaskListRequest req)
        {
            return new TaskListResponse()
            {
                Tasks = this.taskRepository
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => t.CloneWithoutSecrets())
                    .ToList()
            };
        }

        public TaskListByProjectResponse Get(TaskListByProjectRequest req)
        {
            return new TaskListByProjectResponse()
            {
                Tasks = this.taskRepository
                    .Where(t => t.ProjectId == req.ProjectId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => t.CloneWithoutSecrets())
                    .ToList()
            };
        }

        public TaskRetrieveResponse Get(TaskRetrieveRequest req)
        {
            TaskEntity task = this.taskRepository.GetEntityById(req.TaskId).CloneWithoutSecrets();
            List<TaskShellEntity> taskShells = this.taskShellRepository
                .OrderBy(ts => ts.Index).Where(ts => ts.TaskId == task.Id)
                .ToList();

            return new TaskRetrieveResponse()
            {
                Task = task,
                Shell = taskShells
            };
        }
    }
}
