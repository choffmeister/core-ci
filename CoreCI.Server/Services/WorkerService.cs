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
using ServiceStack.ServiceHost;
using CoreCI.Contracts;
using CoreCI.Models;

namespace CoreCI.Server.Services
{
    public class WorkerService : Service
    {
        private static readonly object _taskLock = new object();
        private readonly IRepository<WorkerEntity> _workerRepository;
        private readonly IRepository<TaskEntity> _taskRepository;

        public WorkerService(IRepository<WorkerEntity> workerRepository, IRepository<TaskEntity> taskRepository)
        {
            _workerRepository = workerRepository;
            _taskRepository = taskRepository;
        }

        public override void Dispose()
        {
            _workerRepository.Dispose();
            _taskRepository.Dispose();
        }

        public WorkerKeepAliveResponse Post(WorkerKeepAliveRequest req)
        {
            WorkerEntity worker = _workerRepository.SingleOrDefault(w => w.Id == req.WorkerId);

            if (worker != null)
            {
                Console.WriteLine("[{0}] Living worker", req.WorkerId);

                worker.LastKeepAlive = DateTime.UtcNow;

                _workerRepository.Update(worker);
            }
            else
            {
                Console.WriteLine("[{0}] New worker", req.WorkerId);

                worker = new WorkerEntity()
                {

                    Id = req.WorkerId,
                    LastKeepAlive = DateTime.UtcNow
                };

                _workerRepository.InsertOrUpdate(worker);
            }

            return new WorkerKeepAliveResponse();
        }

        public WorkerGetTaskResponse Post(WorkerGetTaskRequest req)
        {
            lock (_taskLock)
            {
                TaskEntity task = _taskRepository.Where(t => t.State == TaskState.Pending).OrderBy(t => t.CreatedAt).FirstOrDefault();

                if (task != null)
                {
                    Console.WriteLine("[{0}] Delegating task", req.WorkerId);

                    task.State = TaskState.Running;
                    task.DelegatedAt = DateTime.UtcNow;
                    _taskRepository.Update(task);

                    return new WorkerGetTaskResponse(task);
                }

                return new WorkerGetTaskResponse();
            }
        }
    }
}
