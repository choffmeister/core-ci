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
using System.IO;
using System.Net;
using CoreCI.Common;
using NLog;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;
using System;
using NLog.LayoutRenderers.Wrappers;

namespace CoreCI.Server.GitHub
{
    public class GitHubOAuthService : Service
    {
        private readonly static Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly static string _accessTokenUrlTemplate = "https://github.com/login/oauth/access_token?client_id={0}&client_secret={1}&code={2}";
        private readonly static string _createHookUrlTemplate = "https://api.github.com/repos/{0}/{1}/hooks";
        private readonly static string _listHookUrlTemplate = "https://api.github.com/repos/{0}/{1}/hooks";
        private readonly static string _deleteHookUrlTemplate = "https://api.github.com/repos/{0}/{1}/hooks/{2}";
        private readonly IConfigurationProvider _configurationProvider;

        public GitHubOAuthService(IConfigurationProvider configurationProvider)
        {
            _configurationProvider = configurationProvider;
        }

        public override void Dispose()
        {
            _configurationProvider.Dispose();
        }

        public void Get(GitHubOAuthAuthorizeRequest req)
        {
            string clientId = _configurationProvider.GetSettingString("serverGitHubOAuthClientId");
            string clientSecret = _configurationProvider.GetSettingString("serverGitHubOAuthClientSecret");
            string redirectUrl = _configurationProvider.GetSettingString("serverWebAppPublicBaseAddress");
            string hookUrl = _configurationProvider.GetSettingString("serverApiPublicBaseAddress") + "hook/github";
            string accessTokenUrl = string.Format(_accessTokenUrlTemplate, clientId, clientSecret, req.Code);

            GitHubOAuthAccessTokenResponse res = Get<GitHubOAuthAccessTokenResponse>(accessTokenUrl);
            _logger.Info("Access token {0}", res.Access_Token);

            base.Response.AddHeader("Location", redirectUrl);
        }

        private static void CreateHook(string hookUrl, string accessToken)
        {
            string createHookUrl = string.Format(_createHookUrlTemplate, "choffmeister", "hook-test");
            string listHooksUrl = string.Format(_listHookUrlTemplate, "choffmeister", "hook-test");

            // delete all existing hooks to our url
            GitHubListHooksResponse hooks = Get<GitHubListHooksResponse>(listHooksUrl, accessToken);
            foreach (GitHubHookElement hook in hooks.Where(h => h.Config.ContainsKey("url") && h.Config["url"] == hookUrl))
            {
                string deleteHookUrl = string.Format(_deleteHookUrlTemplate, "choffmeister", "hook-test", hook.Id);

                Delete<GitHubDeleteHookResponse>(deleteHookUrl, accessToken);
            }

            // (re)create a hook to our url
            Post<GitHubCreateHookResponse, GitHubCreateHookRequest>(createHookUrl, new GitHubCreateHookRequest()
            {
                Name = "web",
                Active = false,
                Events = new List<string>()
                {
                    "push"
                },
                Config = new Dictionary<string, string>()
                {
                    { "url", hookUrl },
                    { "content_type", "json" }
                }
            }, accessToken);

            _logger.Info("Created hook");
        }

        private static TReceive Get<TReceive>(string url, string accessToken = null)
            where TReceive : class, new()
        {
            return MakeRequest<TReceive, object>(url, "GET", null, accessToken);
        }

        private static TReceive Post<TReceive, TSend>(string url, TSend data, string accessToken = null)
            where TReceive : class, new()
            where TSend : class, new()
        {
            return MakeRequest<TReceive, TSend>(url, "POST", data, accessToken);
        }

        private static TReceive Delete<TReceive>(string url, string accessToken = null)
            where TReceive : class, new()
        {
            return MakeRequest<TReceive, object>(url, "DELETE", null, accessToken);
        }

        private static TReceive MakeRequest<TReceive, TSend>(string url, string method, TSend data, string accessToken = null)
            where TReceive : class, new()
            where TSend : class, new()
        {
            JsonSerializer<TReceive> serializerReceive = new JsonSerializer<TReceive>();
            JsonSerializer<TSend> serializerSend = new JsonSerializer<TSend>();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            request.ContentType = "application/json";
            request.Accept = "application/json";

            if (accessToken != null)
            {
                request.Headers.Add("Authorization", "token " + accessToken);
            }

            if (data != null)
            {
                using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                {
                    serializerSend.SerializeToWriter(data, writer);
                }
            }

            using (WebResponse response = request.GetResponse())
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    return serializerReceive.DeserializeFromReader(reader);
                }
            }
        }
    }

    [Route("/auth/github", "GET")]
    public class GitHubOAuthAuthorizeRequest : IReturnVoid
    {
        public string Code { get; set; }
    }

    public class GitHubOAuthAccessTokenResponse
    {
        public string Access_Token { get; set; }

        public string Token_Type { get; set; }
    }

    public class GitHubCreateHookRequest : IReturn<GitHubCreateHookResponse>
    {
        public string Name { get; set; }

        public bool Active { get; set; }

        public List<string> Events { get; set; }

        public Dictionary<string, string> Config { get; set; }
    }

    public class GitHubCreateHookResponse
    {
    }

    public class GitHubListHooksRequest : IReturn<GitHubCreateHookResponse>
    {
    }

    public class GitHubListHooksResponse : List<GitHubHookElement>
    {
    }

    public class GitHubDeleteHookRequest : IReturn<GitHubDeleteHookResponse>
    {
    }

    public class GitHubDeleteHookResponse
    {
    }

    public class GitHubHookElement
    {
        public long Id { get; set; }

        public bool Active { get; set; }

        public string Name { get; set; }

        public string Url { get; set; }

        public List<string> Events { get; set; }

        public Dictionary<string,string> Config { get; set; }

        public DateTime Created_At { get; set; }

        public DateTime Updated_at { get; set; }
    }
}
