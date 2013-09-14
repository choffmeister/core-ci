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
using CoreCI.Common;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.ServiceInterface.Auth;
using CoreCI.Models;
using System.Collections.Generic;
using NLog;
using System.IO;
using ServiceStack.Common.Web;
using System.Text;
using YamlDotNet.RepresentationModel;
using CoreCI.Server.Services;
using System.Net;
using System.Security.Cryptography;

namespace CoreCI.Server.Connectors
{
    [Connector("github")]
    public class GitHubConnector : IConnector
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IConfigurationProvider _configurationProvider;
        private readonly IUserRepository _userRepository;
        private readonly IConnectorRepository _connectorRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly string _gitHubConsumerKey;
        private readonly string _gitHubConsumerSecret;
        private readonly string _gitHubScopes;
        private readonly string _gitHubRedirectUrl;
        public const string Name = "github";
        public const string AuthorizeUrl = "https://github.com/login/oauth/authorize";
        public const string AccessTokenUrl = "https://github.com/login/oauth/access_token";
        public const string UserProfileUrl = "https://api.github.com/user";
        public const string RepositoriesUrl = "https://api.github.com/user/repos";
        public const string CreateHookUrl = "https://api.github.com/repos/{0}/{1}/hooks";
        public const string ListHooksUrl = "https://api.github.com/repos/{0}/{1}/hooks";
        public const string DeleteHookUrl = "https://api.github.com/repos/{0}/{1}/hooks/{2}";
        public const string CreateKeyUrl = "https://api.github.com/repos/{0}/{1}/keys";
        public const string ListKeysUrl = "https://api.github.com/repos/{0}/{1}/keys";
        public const string DeleteKeyUrl = "https://api.github.com/repos/{0}/{1}/keys/{2}";

        public GitHubConnector(IConfigurationProvider configurationProvider, IUserRepository userRepository, IConnectorRepository connectorRepository, IProjectRepository projectRepository, ITaskRepository taskRepository)
        {
            _configurationProvider = configurationProvider;
            _userRepository = userRepository;
            _connectorRepository = connectorRepository;
            _projectRepository = projectRepository;
            _taskRepository = taskRepository;

            _gitHubConsumerKey = configurationProvider.GetSettingString("oauthGitHubConsumerKey");
            _gitHubConsumerSecret = configurationProvider.GetSettingString("oauthGitHubConsumerSecret");
            _gitHubScopes = configurationProvider.GetSettingString("oauthGitHubScopes", false) ?? string.Empty;
            _gitHubRedirectUrl = configurationProvider.GetSettingString("oauthGitHubRedirectUrl", false);
        }

        public void Dispose()
        {
            _configurationProvider.Dispose();
            _userRepository.Dispose();
            _connectorRepository.Dispose();
            _projectRepository.Dispose();
            _taskRepository.Dispose();
        }

        public object Connect(IAuthSession session, IHttpRequest req)
        {
            Guid userId = Guid.Parse(session.UserAuthId);
            string code = req.QueryString.Get("code");

            if (code != null)
            {
                string accessTokenUrl = AccessTokenUrl
                    .AddQueryParam("client_id", _gitHubConsumerKey)
                    .AddQueryParam("client_secret", _gitHubConsumerSecret)
                    .AddQueryParam("code", code);

                var accessTokenResponse = JsonObject.Parse(accessTokenUrl.GetJsonFromUrl());
                string accessToken = accessTokenResponse ["access_token"];

                if (accessToken != null)
                {
                    var userProfile = GetUserProfile(accessToken);

                    DateTime now = DateTime.UtcNow;
                    UserEntity user = _userRepository.Single(u => u.Id == Guid.Parse(session.UserAuthId));
                    ConnectorEntity connector = _connectorRepository.SingleOrDefault(c => c.Provider == Name && c.UserId == userId);

                    // create new connector if not already existent
                    if (connector == null)
                    {
                        _logger.Info("Create new connector");
                        connector = new ConnectorEntity()
                        {
                            UserId = user.Id,
                            Provider = Name,
                            ProviderUserIdentifier = userProfile["id"],
                            CreatedAt = now
                        };
                    }

                    // update connector information
                    _logger.Info("Update connector");
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
                    _connectorRepository.InsertOrUpdate(connector);

                    return this.Redirect(_gitHubRedirectUrl);
                }

                // TODO: error
                throw new Exception("Could not retrieve access token");
            }
            else
            {
                string authorizeUrl = AuthorizeUrl
                    .AddQueryParam("client_id", _gitHubConsumerKey)
                    .AddQueryParam("scope", _gitHubScopes);

                return this.Redirect(authorizeUrl);
            }
        }

        public object ProcessHook(IHttpRequest req)
        {
            _logger.Info("Received hook");

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

            ProjectEntity project = _projectRepository.SingleOrDefault(p => p.Token == tokenString);

            if (project == null)
                throw new ArgumentException("Invalid token", "token");

            TaskEntity task = new TaskEntity()
            {
                CreatedAt = DateTime.UtcNow,
                ProjectId = project.Id,
                Branch = branchName,
                Commit = commitHash,
                CommitUrl = commit.Child("url"),
                CommitMessage = commit.Child("message"),
                Configuration = GetConfiguration(ownerName, repositoryName, branchName, commitHash)
            };
            _taskRepository.Insert(task);

            PushService.Push("tasks", null);
            PushService.Push("task-" + task.Id.ToString().Replace("-", "").ToLowerInvariant(), "created");

            return null;
        }

        public List<string> ListProjects(IAuthSession session, Guid connectorId)
        {
            ConnectorEntity connector = _connectorRepository.Single(c => c.Id == connectorId);

            if (connector.UserId != Guid.Parse(session.UserAuthId))
                throw HttpError.NotFound("Unknown connector");
            if (connector.Provider != Name)
                throw new InvalidOperationException();

            return GetRepositories(connector.Options ["AccessToken"])
                .Select(r => r.Child("name"))
                .ToList();
        }

        public void AddProject(IAuthSession session, Guid connectorId, string projectName)
        {
            Guid userId = Guid.Parse(session.UserAuthId);
            ConnectorEntity connector = _connectorRepository.Single(c => c.Id == connectorId);

            if (connector.UserId != userId)
                throw HttpError.NotFound("Unknown connector");
            if (connector.Provider != Name)
                throw new InvalidOperationException();

            string gitHubUserName = connector.Options ["UserName"];
            string accessToken = connector.Options ["AccessToken"];
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

            // create RSA key pair
            RSA rsa = new RSACryptoServiceProvider(1024);
            project.Options ["PublicKey"] = rsa.ToOpenSshPublicKeyFileString("test@choffmeister");
            project.Options ["PrivateKey"] = rsa.ToOpenSshPrivateKeyFileString();

            _projectRepository.Insert(project);

            CleanUpHooks(accessToken, gitHubUserName, projectName);
            CreateHook(accessToken, gitHubUserName, projectName, token, project.Id, "http://home.choffmeister.com:8080/api/connector/github/hook");
            CleanUpKeys(accessToken, gitHubUserName, projectName);
            CreateKey(accessToken, gitHubUserName, projectName, project.Options ["PublicKey"]);

            _logger.Info("Created hook");
        }

        private static void CleanUpHooks(string accessToken, string ownerName, string repositoryName)
        {
            // delete all existing hooks to our url
            string listHooksUrl = string.Format(ListHooksUrl, ownerName, repositoryName)
                .AddQueryParam("access_token", accessToken);

            JsonArrayObjects hooks = JsonArrayObjects.Parse(listHooksUrl.GetJsonFromUrl());
            foreach (JsonObject hook in hooks.Where(h => h.Child("url").Contains("choffmeister")))
            {
                string deleteHookUrl = string.Format(DeleteHookUrl, ownerName, repositoryName, hook.Child("id"))
                    .AddQueryParam("access_token", accessToken);

                deleteHookUrl.DeleteFromUrl();
            }
        }

        private static void CreateHook(string accessToken, string ownerName, string repositoryName, string token, Guid projectId, string url)
        {
            string createHookUrl = string.Format(CreateHookUrl, ownerName, repositoryName)
                .AddQueryParam("access_token", accessToken);

            createHookUrl.PostJsonToUrl(new
            {
                name = "web",
                active = false,
                events = new List<string>() { "push" },
                config = new Dictionary<string, string>() { { "url", url.AddQueryParam("token", token) } },
            });
        }

        private static void CleanUpKeys(string accessToken, string ownerName, string repositoryName)
        {
            string listKeysUrl = string.Format(ListKeysUrl, ownerName, repositoryName)
                .AddQueryParam("access_token", accessToken);

            JsonArrayObjects keys = JsonArrayObjects.Parse(listKeysUrl.GetJsonFromUrl());
            foreach (JsonObject key in keys.Where(h => h.Child("title").Contains("choffmeister")))
            {
                string deleteKeyUrl = string.Format(DeleteKeyUrl, ownerName, repositoryName, key.Child("id"))
                    .AddQueryParam("access_token", accessToken);

                deleteKeyUrl.DeleteFromUrl();
            }
        }

        private static void CreateKey(string accessToken, string ownerName, string repositoryName, string publicKeyString)
        {
            string createKeyUrl = string.Format(CreateKeyUrl, ownerName, repositoryName)
                .AddQueryParam("access_token", accessToken);

            createKeyUrl.PostJsonToUrl(new
            {
                key = publicKeyString
            });
        }

        private static JsonArrayObjects GetRepositories(string accessToken)
        {
            string repositoryUrl = RepositoriesUrl.AddQueryParam("access_token", accessToken);
            var repositoriesString = repositoryUrl.GetJsonFromUrl();
            var repositories = JsonArrayObjects.Parse(repositoriesString);

            return repositories;
        }

        private static JsonObject GetUserProfile(string accessToken)
        {
            string userProfileUrl = UserProfileUrl.AddQueryParam("access_token", accessToken);
            var userProfileString = userProfileUrl.GetJsonFromUrl();
            var userProfile = JsonObject.Parse(userProfileString);

            return userProfile;
        }

        private TaskConfiguration GetConfiguration(string ownerName, string repositoryName, string reference, string commitHash)
        {
            string checkoutScript = CreateCheckoutScript(ownerName, repositoryName, reference, commitHash);
            string configurationRaw = GetConfigurationRaw(ownerName, repositoryName, commitHash);

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

        private static string GetConfigurationRaw(string ownerName, string repositoryName, string commitHash)
        {
            // TODO: use GitHub access_token
            string url = string.Format("https://raw.github.com/{0}/{1}/{2}/.core-ci.yml", ownerName, repositoryName, commitHash);

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

        private static string CreateCheckoutScript(string ownerName, string repositoryName, string branchName, string commitHash)
        {
            StringBuilder script = new StringBuilder();

            script.Append(string.Format("git clone --depth=50 --branch={2} git://github.com/{0}/{1}.git {0}/{1}\n", ownerName, repositoryName, branchName, commitHash));
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

        private static string NormalizeGuid(Guid guid)
        {
            return guid.ToString().Replace("-", "").ToLower();
        }
    }
}
