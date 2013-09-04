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
using ServiceStack.Common.Web;
using NLog;

namespace CoreCI.Server.Services
{
    public class DispatcherService : Service
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static readonly object _taskLock = new object();
        private readonly IWorkerRepository _workerRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly ITaskShellRepository _taskShellRepository;

        public DispatcherService(IWorkerRepository workerRepository, ITaskRepository taskRepository, ITaskShellRepository taskShellRepository)
        {
            _workerRepository = workerRepository;
            _taskRepository = taskRepository;
            _taskShellRepository = taskShellRepository;
        }

        public override void Dispose()
        {
            _workerRepository.Dispose();
            _taskRepository.Dispose();
            _taskShellRepository.Dispose();
        }

        public DispatcherWorkerKeepAliveResponse Post(DispatcherWorkerKeepAliveRequest req)
        {
            WorkerEntity worker = _workerRepository.SingleOrDefault(w => w.Id == req.WorkerId);

            if (worker != null)
            {
                _logger.Trace("Worker {0} alive", req.WorkerId);

                worker.LastKeepAlive = DateTime.UtcNow;

                _workerRepository.Update(worker);
            }
            else
            {
                _logger.Info("New worker {0} registered", req.WorkerId);

                worker = new WorkerEntity()
                {

                    Id = req.WorkerId,
                    LastKeepAlive = DateTime.UtcNow
                };

                _workerRepository.InsertOrUpdate(worker);
            }

            return new DispatcherWorkerKeepAliveResponse();
        }

        public DispatcherTaskPollResponse Post(DispatcherTaskPollRequest req)
        {
            lock (_taskLock)
            {
                TaskEntity task = _taskRepository.GetPendingTask(req.WorkerId);

                if (task != null)
                {
                    _logger.Info("Delegating task {0} to worker {1}", task.Id, req.WorkerId);

                    PushService.Push("tasks", null);
                    PushService.Push("task-" + task.Id.ToString().Replace("-", "").ToLowerInvariant(), "started");

                    return new DispatcherTaskPollResponse(task);
                }

                return new DispatcherTaskPollResponse();
            }
        }

        public DispatcherTaskUpdateStartResponse Post(DispatcherTaskUpdateStartRequest req)
        {
            TaskEntity task = _taskRepository.Single(t => t.Id == req.TaskId);

            if (task == null)
            {
                throw HttpError.NotFound(string.Format("Could not find task {0}", req.TaskId));
            }

            _logger.Info("Task {0} started", req.TaskId);

            task.StartedAt = DateTime.UtcNow;
            _taskRepository.Update(task);

            PushService.Push("tasks", null);
            PushService.Push("task-" + task.Id.ToString().Replace("-", "").ToLowerInvariant(), "finished");

            return new DispatcherTaskUpdateStartResponse();
        }

        public DispatcherTaskUpdateFinishResponse Post(DispatcherTaskUpdateFinishRequest req)
        {
            TaskEntity task = _taskRepository.Single(t => t.Id == req.TaskId);

            if (task == null)
            {
                throw HttpError.NotFound(string.Format("Could not find task {0}", req.TaskId));
            }

            _logger.Info("Task {0} finished with exit code {1}", req.TaskId, req.ExitCode);

            task.FinishedAt = DateTime.UtcNow;
            task.ExitCode = req.ExitCode;
            task.State = task.ExitCode == 0 ? TaskState.Succeeded : TaskState.Failed;
            _taskRepository.Update(task);

            PushService.Push("tasks", null);
            PushService.Push("task-" + task.Id.ToString().Replace("-", "").ToLowerInvariant(), "finished");

            return new DispatcherTaskUpdateFinishResponse();
        }

        public DispatcherTaskUpdateShellResponse Post(DispatcherTaskUpdateShellRequest req)
        {
            TaskEntity task = _taskRepository.Single(t => t.Id == req.TaskId);

            if (task == null)
            {
                throw HttpError.NotFound(string.Format("Could not find task {0}", req.TaskId));
            }

            TaskShellEntity taskShell = _taskShellRepository.SingleOrDefault(ts => ts.TaskId == task.Id);

            if (taskShell == null)
            {
                taskShell = new TaskShellEntity()
                {
                    TaskId = task.Id
                };
                _taskShellRepository.Insert(taskShell);
            }

            foreach (var line in req.Lines)
            {
                taskShell.Output.Add(line);
            }
            _taskShellRepository.Update(taskShell);

            PushService.Push("tasks", null);
            PushService.Push("task-" + task.Id.ToString().Replace("-", "").ToLowerInvariant(), "updated");

            return new DispatcherTaskUpdateShellResponse();
        }
    }
}
