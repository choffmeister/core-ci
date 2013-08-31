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
using ServiceStack.ServiceInterface;
using CoreCI.Contracts;
using NLog;
using ServiceStack.Text;
using ServiceStack.ServiceHost;
using System.IO;
using CoreCI.Models;

namespace CoreCI.Server.Services
{
    /// <summary>
    /// Service that allows other applications like GitHub
    /// to invoke tasks.
    /// </summary>
    public class HookGitHubService : Service
    {
        private readonly static Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IRepository<TaskEntity> _taskRepository;

        public HookGitHubService(IRepository<TaskEntity> taskRepository)
        {
            _taskRepository = taskRepository;
        }

        public override void Dispose()
        {
            _taskRepository.Dispose();
        }

        public HookGitHubResponse Post(HookGitHubRequest req)
        {
            JsonSerializer<HookGitHubRequestPayload> serializer = new JsonSerializer<HookGitHubRequestPayload>();
            HookGitHubRequestPayload payload = serializer.DeserializeFromString(this.Request.FormData ["payload"]);

            _logger.Info("Received hook from GitHub for repository {0}/{1} with commit ID {2}", payload.Repository.Owner.Name, payload.Repository.Name, payload.After);

            TaskEntity task = new TaskEntity()
            {
                Script = CreateTaskScript(payload.Repository.Owner.Name, payload.Repository.Name, payload.Ref, payload.After),
                CreatedAt = DateTime.UtcNow
            };
            _taskRepository.Insert(task);

            PushService.Push("tasks", null);
            PushService.Push("task-" + task.Id.ToString().Replace("-", "").ToLowerInvariant(), "created");

            return new HookGitHubResponse();
        }

        private static string CreateTaskScript(string repositoryOwnerName, string repositoryName, string reference, string commitHash)
        {
            if (!reference.StartsWith("refs/heads/"))
            {
                throw new Exception("Reference must start with refs/heads/");
            }

            string branch = reference.Substring("refs/heads/".Length);

            return string.Format(@"git clone --depth=50 --branch={2} git://github.com/{0}/{1}.git {0}/{1}
cd {0}/{1} && git checkout -qf {3}
cd {0}/{1} && git branch -va
sudo apt-get update", repositoryOwnerName, repositoryName, branch, commitHash);
        }
    }
}
