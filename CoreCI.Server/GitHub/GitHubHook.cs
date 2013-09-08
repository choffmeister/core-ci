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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using CoreCI.Models;
using NLog;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using YamlDotNet.RepresentationModel;
using CoreCI.Server.Hooks;
using CoreCI.Server.Services;

namespace CoreCI.Server.Hooks
{
    [Hook("github")]
    public class GitHubHook : IHook
    {
        private readonly static Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IProjectRepository _projectRepository;
        private readonly ITaskRepository _taskRepository;

        public GitHubHook(IProjectRepository projectRepository, ITaskRepository taskRepository)
        {
            _projectRepository = projectRepository;
            _taskRepository = taskRepository;
        }

        public void Dispose()
        {
            _projectRepository.Dispose();
            _taskRepository.Dispose();
        }

        public void Process(IHttpRequest request)
        {
            JsonSerializer<GitHubHookPayload> serializer = new JsonSerializer<GitHubHookPayload>();
            GitHubHookPayload payload = serializer.DeserializeFromString(request.FormData ["payload"]);
            string projectName = string.Format("{0}/{1}", payload.Repository.Owner.Name, payload.Repository.Name);
            string branch = ConvertReferenceToBranch(payload.Ref);
            GitHubHookPayload.PayloadCommit commit = payload.Commits.Single(c => c.Id == payload.After);

            _logger.Info("Received hook from GitHub for repository {0} with commit ID {1}", projectName, commit.Id);

            ProjectEntity project = CreateOrGetProject(projectName);
            TaskEntity task = CreateTask(project.Id, branch, payload, commit);

            PushService.Push("tasks", null);
            PushService.Push("task-" + task.Id.ToString().Replace("-", "").ToLowerInvariant(), "created");
        }

        private ProjectEntity CreateOrGetProject(string name)
        {
            ProjectEntity project = _projectRepository.SingleOrDefault(p => p.Name == name);

            if (project == null)
            {
                project = new ProjectEntity()
                {
                    Id = Guid.NewGuid(),
                    Name = name
                };
                _projectRepository.Insert(project);

                PushService.Push("projects", null);
            }

            return project;
        }

        private TaskEntity CreateTask(Guid projectId, string branch, GitHubHookPayload payload, GitHubHookPayload.PayloadCommit commit)
        {
            TaskEntity task = new TaskEntity()
            {
                CreatedAt = DateTime.UtcNow,
                ProjectId = projectId,
                Branch = branch,
                Commit = commit.Id,
                CommitUrl = commit.Url,
                CommitMessage = commit.Message,
                Configuration = GetConfiguration(payload.Repository.Owner.Name, payload.Repository.Name, branch, payload.After)
            };
            _taskRepository.Insert(task);

            return task;
        }

        private static string GetConfigurationRaw(string repositoryOwnerName, string repositoryName, string commitHash)
        {
            string url = string.Format("https://raw.github.com/{0}/{1}/{2}/.core-ci.yml", repositoryOwnerName, repositoryName, commitHash);

            try
            {
                _logger.Trace("Loading configuration from {0}", url);

                HttpWebRequest configRequest = (HttpWebRequest)WebRequest.Create(url);
                using (WebResponse configResponse = configRequest.GetResponse())
                {
                    using (StreamReader configReader = new StreamReader(configResponse.GetResponseStream()))
                    {
                        return configReader.ReadToEnd();
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError && ex.Response != null)
                {
                    var resp = (HttpWebResponse)ex.Response;
                    if (resp.StatusCode == HttpStatusCode.NotFound)
                    {
                        return null;
                    }
                }

                _logger.Error(ex);

                throw ex;
            }
        }

        private TaskConfiguration GetConfiguration(string repositoryOwnerName, string repositoryName, string reference, string commitHash)
        {
            string checkoutScript = CreateCheckoutScript(repositoryOwnerName, repositoryName, reference, commitHash);
            string configurationRaw = GetConfigurationRaw(repositoryOwnerName, repositoryName, commitHash);

            if (configurationRaw != null)
            {
                var yaml = new YamlStream();
                var configReader = new StringReader(configurationRaw);
                yaml.Load(configReader);
                var rootNode = (YamlMappingNode)yaml.Documents [0].RootNode;

                string machine = ((YamlScalarNode)rootNode.Children [new YamlScalarNode("machine")]).Value;
                string script = ((YamlScalarNode)rootNode.Children [new YamlScalarNode("script")]).Value;

                return new TaskConfiguration()
                {
                    Machine = machine,
                    CheckoutScript = checkoutScript,
                    TestScript = script
                };
            }
            else
            {
                // project has no configuration file, use default configuration
                return new TaskConfiguration()
                {
                    Machine = "precise64",
                    CheckoutScript = checkoutScript,
                    TestScript = ""
                };
            }
        }

        private static string CreateCheckoutScript(string repositoryOwnerName, string repositoryName, string branch, string commitHash)
        {
            StringBuilder script = new StringBuilder();

            script.Append(string.Format("git clone --depth=50 --branch={2} git://github.com/{0}/{1}.git {0}/{1}\n", repositoryOwnerName, repositoryName, branch, commitHash));
            script.Append(string.Format("cd {0}/{1} && git checkout -qf {3}\n", repositoryOwnerName, repositoryName, branch, commitHash));
            script.Append(string.Format("cd {0}/{1} && git branch -va\n", repositoryOwnerName, repositoryName, branch, commitHash));

            return script.ToString();
        }

        private static string ConvertReferenceToBranch(string reference)
        {
            if (!reference.StartsWith("refs/heads/", StringComparison.InvariantCulture))
            {
                throw new Exception("Reference must start with refs/heads/");
            }

            return reference.Substring("refs/heads/".Length);
        }
    }

    public class GitHubHookPayload
    {
        public string After { get; set; }

        public string Before { get; set; }

        public List<PayloadCommit> Commits { get; set; }

        public PayloadRepository Repository { get; set; }

        public string Ref { get; set; }

        public GitHubHookPayload()
        {
            this.Commits = new List<PayloadCommit>();
        }

        public class PayloadCommit
        {
            public string Id { get; set; }

            public string Message { get; set; }

            public string Url { get; set; }
        }

        public class PayloadRepository
        {
            public string Name { get; set; }

            public PayloadRepositoryOwner Owner { get; set; }
        }

        public class PayloadRepositoryOwner
        {
            public string Name { get; set; }
        }
    }
}
