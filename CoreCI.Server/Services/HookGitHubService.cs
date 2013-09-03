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
using CoreCI.Contracts;
using NLog;
using ServiceStack.Text;
using ServiceStack.ServiceHost;
using System.IO;
using CoreCI.Models;
using System.Net;
using YamlDotNet.RepresentationModel;
using System.Text;

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
            HookGitHubRequestPayload.PayloadCommit commit = payload.Commits.Single(c => c.Id == payload.After);

            _logger.Info("Received hook from GitHub for repository {0}/{1} with commit ID {2}", payload.Repository.Owner.Name, payload.Repository.Name, payload.After);

            TaskEntity task = new TaskEntity()
            {
                Configuration = GetConfiguration(payload.Repository.Owner.Name, payload.Repository.Name, payload.Ref, payload.After),
                CreatedAt = DateTime.UtcNow,
                Commit = commit.Id,
                CommitUrl = commit.Url,
                CommitMessage = commit.Message
            };
            _taskRepository.Insert(task);

            PushService.Push("tasks", null);
            PushService.Push("task-" + task.Id.ToString().Replace("-", "").ToLowerInvariant(), "created");

            return new HookGitHubResponse();
        }

        private static TaskConfiguration GetConfiguration(string repositoryOwnerName, string repositoryName, string reference, string commitHash)
        {
            string url = string.Format("https://raw.github.com/{0}/{1}/{2}/.core-ci.yml", repositoryOwnerName, repositoryName, commitHash);
            string checkoutScript = CreateCheckoutScript(repositoryOwnerName, repositoryName, reference, commitHash);

            try
            {
                _logger.Trace("Loading configuration from {0}", url);

                HttpWebRequest configRequest = (HttpWebRequest)WebRequest.Create(url);
                using (WebResponse configResponse = configRequest.GetResponse())
                {
                    using (StreamReader configReader = new StreamReader(configResponse.GetResponseStream()))
                    {
                        var yaml = new YamlStream();
                        yaml.Load(configReader);
                        var rootNode = (YamlMappingNode)yaml.Documents [0].RootNode;

                        string machine = ((YamlScalarNode)rootNode.Children [new YamlScalarNode("machine")]).Value;
                        string script = ((YamlScalarNode)rootNode.Children [new YamlScalarNode("script")]).Value;

                        return new TaskConfiguration()
                        {
                            Machine = machine,
                            Script = checkoutScript + script
                        };
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
                        // project has no configuration file, use default configuration
                        return new TaskConfiguration()
                        {
                            Machine = "precise64",
                            Script = checkoutScript
                        };
                    }
                }

                _logger.Error(ex);

                throw ex;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);

                throw ex;
            }
        }

        private static string CreateCheckoutScript(string repositoryOwnerName, string repositoryName, string reference, string commitHash)
        {
            if (!reference.StartsWith("refs/heads/"))
            {
                throw new Exception("Reference must start with refs/heads/");
            }

            string branch = reference.Substring("refs/heads/".Length);
            StringBuilder script = new StringBuilder();

            script.Append(string.Format("git clone --depth=50 --branch={2} git://github.com/{0}/{1}.git {0}/{1}\n", repositoryOwnerName, repositoryName, branch, commitHash));
            script.Append(string.Format("cd {0}/{1} && git checkout -qf {3}\n", repositoryOwnerName, repositoryName, branch, commitHash));
            script.Append(string.Format("cd {0}/{1} && git branch -va\n", repositoryOwnerName, repositoryName, branch, commitHash));

            return script.ToString();
        }
    }
}
