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
using CoreCI.Contracts;
using CoreCI.Models;
using NLog;
using ServiceStack.ServiceInterface;

namespace CoreCI.Server.Services
{
    public class DispatcherService : Service
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly IWorkerRepository workerRepository;
        private readonly ITaskRepository taskRepository;
        private readonly ITaskShellRepository taskShellRepository;

        public DispatcherService(IWorkerRepository workerRepository, ITaskRepository taskRepository, ITaskShellRepository taskShellRepository)
        {
            this.workerRepository = workerRepository;
            this.taskRepository = taskRepository;
            this.taskShellRepository = taskShellRepository;
        }

        public override void Dispose()
        {
            this.workerRepository.Dispose();
            this.taskRepository.Dispose();
            this.taskShellRepository.Dispose();
        }

        public DispatcherWorkerKeepAliveResponse Post(DispatcherWorkerKeepAliveRequest req)
        {
            WorkerEntity worker = this.workerRepository.SingleOrDefault(w => w.Id == req.WorkerId);

            if (worker != null)
            {
                Log.Trace("Worker {0} alive", req.WorkerId);

                worker.LastKeepAlive = DateTime.UtcNow;

                this.workerRepository.Update(worker);
            }
            else
            {
                Log.Info("New worker {0} registered", req.WorkerId);

                worker = new WorkerEntity()
                {
                    Id = req.WorkerId,
                    LastKeepAlive = DateTime.UtcNow
                };

                this.workerRepository.InsertOrUpdate(worker);
            }

            return new DispatcherWorkerKeepAliveResponse();
        }

        public DispatcherTaskPollResponse Post(DispatcherTaskPollRequest req)
        {
            TaskEntity task = this.taskRepository.GetPendingTask(req.WorkerId);

            if (task != null)
            {
                Log.Info("Delegating task {0} to worker {1}", task.Id, req.WorkerId);

                PushService.Push("tasks", null);
                PushService.Push("task-" + task.Id.ToString().Replace("-", string.Empty).ToLowerInvariant(), "started");

                return new DispatcherTaskPollResponse(task);
            }

            return new DispatcherTaskPollResponse();
        }

        public DispatcherTaskUpdateStartResponse Post(DispatcherTaskUpdateStartRequest req)
        {
            TaskEntity task = this.taskRepository.GetEntityById(req.TaskId);
            Log.Info("Task {0} started", req.TaskId);

            task.StartedAt = DateTime.UtcNow;
            this.taskRepository.Update(task);

            PushService.Push("tasks", null);
            PushService.Push("task-" + task.Id.ToString().Replace("-", string.Empty).ToLowerInvariant(), "finished");

            return new DispatcherTaskUpdateStartResponse();
        }

        public DispatcherTaskUpdateFinishResponse Post(DispatcherTaskUpdateFinishRequest req)
        {
            TaskEntity task = this.taskRepository.GetEntityById(req.TaskId);
            Log.Info("Task {0} finished with exit code {1}", req.TaskId, req.ExitCode);

            task.FinishedAt = DateTime.UtcNow;
            task.ExitCode = req.ExitCode;
            task.State = task.ExitCode == 0 ? TaskState.Succeeded : TaskState.Failed;
            this.taskRepository.Update(task);

            PushService.Push("tasks", null);
            PushService.Push("task-" + task.Id.ToString().Replace("-", string.Empty).ToLowerInvariant(), "finished");

            return new DispatcherTaskUpdateFinishResponse();
        }

        public DispatcherTaskUpdateShellResponse Post(DispatcherTaskUpdateShellRequest req)
        {
            TaskEntity task = this.taskRepository.GetEntityById(req.TaskId);
            TaskShellEntity taskShell = this.taskShellRepository.SingleOrDefault(ts => ts.TaskId == task.Id && ts.Index == req.Index);

            if (taskShell == null)
            {
                taskShell = new TaskShellEntity()
                {
                    TaskId = task.Id,
                    Index = req.Index,
                    Output = req.Output
                };
                this.taskShellRepository.Insert(taskShell);
            }
            else
            {
                taskShell.Output = req.Output;
                this.taskShellRepository.Update(taskShell);
            }

            PushService.Push("tasks", null);
            PushService.Push("task-" + task.Id.ToString().Replace("-", string.Empty).ToLowerInvariant(), "updated");

            return new DispatcherTaskUpdateShellResponse();
        }
    }
}
