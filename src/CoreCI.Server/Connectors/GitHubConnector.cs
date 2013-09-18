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
using System.Security.Cryptography;
using System.Text;
using CoreCI.Common;
using CoreCI.Models;
using CoreCI.Server.Services;
using NLog;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Text;
using YamlDotNet.RepresentationModel;

namespace CoreCI.Server.Connectors
{
    [Connector("github")]
    public class GitHubConnector : IConnector
    {
        public const string Name = "github";
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly IConfigurationProvider configurationProvider;
        private readonly IUserRepository userRepository;
        private readonly IConnectorRepository connectorRepository;
        private readonly IProjectRepository projectRepository;
        private readonly ITaskRepository taskRepository;
        private readonly string serverDomainPublic;
        private readonly string serverApiPublicBaseAddress;
        private readonly string gitHubConsumerKey;
        private readonly string gitHubConsumerSecret;
        private readonly string[] gitHubScopes;
        private readonly string gitHubRedirectUrl;

        public GitHubConnector(IConfigurationProvider configurationProvider, IUserRepository userRepository, IConnectorRepository connectorRepository, IProjectRepository projectRepository, ITaskRepository taskRepository)
        {
            this.configurationProvider = configurationProvider;
            this.userRepository = userRepository;
            this.connectorRepository = connectorRepository;
            this.projectRepository = projectRepository;
            this.taskRepository = taskRepository;

            this.serverDomainPublic = configurationProvider.Get("server.addresses.public.domain");
            this.serverApiPublicBaseAddress = configurationProvider.Get("server.addresses.public.api");

            this.gitHubConsumerKey = configurationProvider.Get("server.github.oauth2.consumer.key");
            this.gitHubConsumerSecret = configurationProvider.Get("server.github.oauth2.consumer.secret");
            this.gitHubScopes = configurationProvider.GetArray("server.github.oauth2.scopes") ?? new string[0];
            this.gitHubRedirectUrl = configurationProvider.Get("server.github.oauth2.redirect");
        }

        public void Dispose()
        {
            this.configurationProvider.Dispose();
            this.userRepository.Dispose();
            this.connectorRepository.Dispose();
            this.projectRepository.Dispose();
            this.taskRepository.Dispose();
        }

        public object Connect(IAuthSession session, IHttpRequest req)
        {
            Guid userId = Guid.Parse(session.UserAuthId);
            string code = req.QueryString.Get("code");

            if (code != null)
            {
                string accessToken = GitHubOAuth2Client.GetAccessToken(this.gitHubConsumerKey, this.gitHubConsumerSecret, code);

                if (accessToken != null)
                {
                    var userProfile = GitHubOAuth2Client.GetUserProfile(accessToken);

                    DateTime now = DateTime.UtcNow;
                    UserEntity user = this.userRepository.Single(u => u.Id == Guid.Parse(session.UserAuthId));
                    ConnectorEntity connector = this.connectorRepository.SingleOrDefault(c => c.Provider == Name && c.UserId == userId);

                    // create new connector if not already existent
                    if (connector == null)
                    {
                        Log.Info("Create new connector");
                        connector = new ConnectorEntity()
                        {
                            UserId = user.Id,
                            Provider = Name,
                            ProviderUserIdentifier = userProfile["id"],
                            CreatedAt = now
                        };
                    }

                    // update connector information
                    Log.Info("Update connector");
                    connector.Options = new Dictionary<string, string>()
                    {
                        { "AccessToken", accessToken },
                        { "UserId", userProfile["id"] },
                        { "UserName", userProfile["login"] },
                        { "Email", userProfile["email"] },
                        { "DisplayName", userProfile["name"] },
                    };
                    connector.ModifiedAt = now;

                    // insert or update the connector
                    this.connectorRepository.InsertOrUpdate(connector);

                    return this.Redirect(this.gitHubRedirectUrl);
                }

                // TODO: error
                throw new Exception("Could not retrieve access token");
            }
            else
            {
                return this.Redirect(GitHubOAuth2Client.GetAuthorizeUrl(this.gitHubConsumerKey, this.gitHubScopes.Join(",")));
            }
        }

        public object ProcessHook(IHttpRequest req)
        {
            Log.Info("Received hook");

            string tokenString = req.GetParam("token");
            string payloadString = req.GetParam("payload");

            if (tokenString == null)
                throw new ArgumentException("Hook request misses token", "token");
            if (payloadString == null)
                throw new ArgumentException("Payload request misses payload", "payload");

            JsonObject payload = JsonObject.Parse(payloadString);
            string ownerName = payload.Object("repository").Object("owner").Child("name");
            string repositoryName = payload.Object("repository").Child("name");
            string commitHash = payload.Child("after");
            JsonObject commit = payload.ArrayObjects("commits").Single(n => n.Child("id") == commitHash);
            string reference = payload.Child("ref");
            string branchName = ConvertReferenceToBranch(reference);

            ProjectEntity project = this.projectRepository.SingleOrDefault(p => p.Token == tokenString);

            if (project == null)
                throw new ArgumentException("Invalid token", "token");

            string publicKeyFileString = project.Options["PublicKey"];
            string privateKeyFileString = project.Options["PrivateKey"];

            ConnectorEntity connector = this.connectorRepository.Single(c => c.Id == project.ConnectorId);

            string accessToken = connector.Options["AccessToken"];

            TaskEntity task = new TaskEntity()
            {
                CreatedAt = DateTime.UtcNow,
                ProjectId = project.Id,
                Branch = branchName,
                Commit = commitHash,
                CommitUrl = commit.Child("url"),
                CommitMessage = commit.Child("message"),
                Configuration = this.GetConfiguration(accessToken, ownerName, repositoryName, branchName, commitHash, publicKeyFileString, privateKeyFileString)
            };
            this.taskRepository.Insert(task);

            PushService.Push("tasks", null);
            PushService.Push("task-" + task.Id.ToUndashedString(), "created");

            return null;
        }

        public List<string> ListProjects(IAuthSession session, Guid connectorId)
        {
            ConnectorEntity connector = this.connectorRepository.Single(c => c.Id == connectorId);

            if (connector.UserId != Guid.Parse(session.UserAuthId))
                throw HttpError.NotFound("Unknown connector");
            if (connector.Provider != Name)
                throw new InvalidOperationException();

            return GitHubOAuth2Client.GetRepositories(connector.Options["AccessToken"])
                .Select(r => r.Child("name"))
                .ToList();
        }

        public ProjectEntity AddProject(IAuthSession session, Guid connectorId, string projectName)
        {
            Guid userId = Guid.Parse(session.UserAuthId);
            ConnectorEntity connector = this.connectorRepository.Single(c => c.Id == connectorId);

            if (connector.UserId != userId)
                throw HttpError.NotFound("Unknown connector");
            if (connector.Provider != Name)
                throw new InvalidOperationException();

            string gitHubUserName = connector.Options["UserName"];
            string accessToken = connector.Options["AccessToken"];
            string token = this.GenerateToken();

            // TODO: retrieve project information from GitHub

            // create project
            ProjectEntity project = new ProjectEntity()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ConnectorId = connectorId,
                Name = projectName,
                FullName = string.Format("{0}/{1}", gitHubUserName, projectName),
                Token = token,
                IsPrivate = false
            };

            string projectIdString = project.Id.ToUndashedString();

            // create RSA key pair
            RSA rsa = new RSACryptoServiceProvider(1024);
            project.Options["PublicKey"] = rsa.ToOpenSshPublicKeyFileString(string.Format("{0}@{1}", projectIdString, this.serverDomainPublic));
            project.Options["PrivateKey"] = rsa.ToOpenSshPrivateKeyFileString();

            this.projectRepository.Insert(project);

            GitHubOAuth2Client.RemoveHooks(accessToken, gitHubUserName, project.Name, h => h.Object("config").Child("url").Contains(projectIdString));
            GitHubOAuth2Client.RemoveKeys(accessToken, gitHubUserName, project.Name, k => k.Child("title").Contains(projectIdString));
            GitHubOAuth2Client.CreateHook(accessToken, gitHubUserName, project.Name, string.Format("{0}connector/github/hook?project={1}&token={2}", this.serverApiPublicBaseAddress, projectIdString, token));
            GitHubOAuth2Client.CreateKey(accessToken, gitHubUserName, project.Name, project.Options["PublicKey"]);

            Log.Info("Created hook");
            return project;
        }

        public void RemoveProject(IAuthSession session, Guid connectorId, Guid projectId)
        {
            Guid userId = Guid.Parse(session.UserAuthId);
            ConnectorEntity connector = this.connectorRepository.Single(c => c.Id == connectorId);
            ProjectEntity project = this.projectRepository.Single(p => p.Id == projectId);

            if (connector.UserId != userId)
                throw HttpError.NotFound("Unknown connector");
            if (connector.Provider != Name)
                throw new InvalidOperationException();

            string projectIdString = project.Id.ToUndashedString();
            string gitHubUserName = connector.Options["UserName"];
            string accessToken = connector.Options["AccessToken"];

            GitHubOAuth2Client.RemoveHooks(accessToken, gitHubUserName, project.Name, h => h.Object("config").Child("url").Contains(projectIdString));
            GitHubOAuth2Client.RemoveKeys(accessToken, gitHubUserName, project.Name, k => k.Child("title").Contains(projectIdString));

            this.projectRepository.Delete(project);
        }

        private TaskConfiguration GetConfiguration(string accessToken, string ownerName, string repositoryName, string reference, string commitHash, string publicKeyFileString, string privateKeyFileString)
        {
            string secretStartupScript = CreateSecretStartupScript(publicKeyFileString, privateKeyFileString);
            string checkoutScript = CreateCheckoutScript(ownerName, repositoryName, reference, commitHash);
            string configurationRaw = GitHubOAuth2Client.GetContent(accessToken, ownerName, repositoryName, ".core-ci.yml", commitHash);

            if (configurationRaw != null)
            {
                var yaml = new YamlStream();
                var configReader = new StringReader(configurationRaw);
                yaml.Load(configReader);
                var rootNode = (YamlMappingNode)yaml.Documents[0].RootNode;

                string machine = ((YamlScalarNode)rootNode.Children[new YamlScalarNode("machine")]).Value;
                string script = ((YamlScalarNode)rootNode.Children[new YamlScalarNode("script")]).Value;

                return new TaskConfiguration()
                {
                    Machine = machine,
                    SecretStartupScript = secretStartupScript,
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
                    SecretStartupScript = secretStartupScript,
                    CheckoutScript = checkoutScript,
                    TestScript = string.Empty
                };
            }
        }

        private static string CreateSecretStartupScript(string publicKeyFileString, string privateKeyFileString)
        {
            StringBuilder script = new StringBuilder();

            script.Append("mkdir -p .ssh\n");
            script.Append(string.Format("echo \"{0}\" > .ssh/idthis.rsa.pub\n", publicKeyFileString));
            script.Append(string.Format("echo \"{0}\" > .ssh/id_rsa\n", privateKeyFileString));
            script.Append("chmod 600 .ssh/id_rsa\n");
            script.Append("chmod 644 .ssh/idthis.rsa.pub\n");
            script.Append("ssh-keyscan -H github.com >> .ssh/known_hosts");

            return script.ToString();
        }

        private static string CreateCheckoutScript(string ownerName, string repositoryName, string branchName, string commitHash)
        {
            StringBuilder script = new StringBuilder();

            script.Append(string.Format("git clone --depth=50 --branch={2} git@github.com:{0}/{1}.git {0}/{1}\n", ownerName, repositoryName, branchName, commitHash));
            script.Append(string.Format("cd {0}/{1} && git checkout -qf {3}\n", ownerName, repositoryName, branchName, commitHash));
            script.Append(string.Format("cd {0}/{1} && git branch -va\n", ownerName, repositoryName, branchName, commitHash));

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
}
