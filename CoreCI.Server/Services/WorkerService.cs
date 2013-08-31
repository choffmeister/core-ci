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
    public class WorkerService : Service
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static readonly object _taskLock = new object();
        private readonly IRepository<WorkerEntity> _workerRepository;
        private readonly IRepository<TaskEntity> _taskRepository;
        private readonly IRepository<TaskShellEntity> _taskShellRepository;

        public WorkerService(IRepository<WorkerEntity> workerRepository, IRepository<TaskEntity> taskRepository, IRepository<TaskShellEntity> taskShellRepository)
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

        public WorkerKeepAliveResponse Post(WorkerKeepAliveRequest req)
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

            return new WorkerKeepAliveResponse();
        }

        public WorkerGetTaskResponse Post(WorkerGetTaskRequest req)
        {
            lock (_taskLock)
            {
                TaskEntity task = _taskRepository.Where(t => t.State == TaskState.Pending).OrderBy(t => t.CreatedAt).FirstOrDefault();

                if (task != null)
                {
                    _logger.Info("Delegating task {0} to worker {1}", task.Id, req.WorkerId);

                    task.State = TaskState.Running;
                    task.DelegatedAt = DateTime.UtcNow;
                    _taskRepository.Update(task);

                    PushService.Push("tasks", null);
                    PushService.Push("task-" + task.Id.ToString().Replace("-", "").ToLowerInvariant(), "started");

                    return new WorkerGetTaskResponse(task);
                }

                return new WorkerGetTaskResponse();
            }
        }

        public WorkerUpdateTaskResponse Post(WorkerUpdateTaskRequest req)
        {
            TaskEntity task = _taskRepository.Single(t => t.Id == req.TaskId);

            if (task == null)
            {
                throw HttpError.NotFound(string.Format("Could not find task {0}", req.TaskId));
            }

            _logger.Info("Task {0} finished with exit code {1}", req.TaskId, req.ExitCode);

            task.ExitCode = req.ExitCode;
            task.State = task.ExitCode == 0 ? TaskState.Succeeded : TaskState.Failed;
            _taskRepository.Update(task);

            PushService.Push("tasks", null);
            PushService.Push("task-" + task.Id.ToString().Replace("-", "").ToLowerInvariant(), "finished");

            return new WorkerUpdateTaskResponse();
        }

        public WorkerUpdateTaskShellResponse Post(WorkerUpdateTaskShellRequest req)
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
                // TODO: remove
                Console.WriteLine("[{0:0000}] {2} {1}", line.Index, line.Content, line.Type == ShellLineType.StandardInput ? ">" : "<");
                taskShell.Output.Add(line);
            }
            _taskShellRepository.Update(taskShell);

            PushService.Push("tasks", null);
            PushService.Push("task-" + task.Id.ToString().Replace("-", "").ToLowerInvariant(), "updated");

            return new WorkerUpdateTaskShellResponse();
        }
    }
}
