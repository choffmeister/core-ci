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

namespace CoreCI.Server.GitHub
{
    public class GitHubConnector : Service
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IConfigurationProvider _configurationProvider;
        private readonly IUserRepository _userRepository;
        private readonly IConnectorRepository _connectorRepository;
        private readonly string _gitHubConsumerKey;
        private readonly string _gitHubConsumerSecret;
        private readonly string _gitHubScopes;
        private readonly string _gitHubRedirectUrl;
        public const string Name = "GitHub";
        public const string AuthorizeUrl = "https://github.com/login/oauth/authorize";
        public const string AccessTokenUrl = "https://github.com/login/oauth/access_token";
        public const string UserProfileUrl = "https://api.github.com/user";
        public const string RepositoriesUrl = "https://api.github.com/repos";

        public GitHubConnector(IConfigurationProvider configurationProvider, IUserRepository userRepository, IConnectorRepository connectorRepository)
        {
            _configurationProvider = configurationProvider;
            _userRepository = userRepository;
            _connectorRepository = connectorRepository;

            _gitHubConsumerKey = configurationProvider.GetSettingString("oauthGitHubConsumerKey");
            _gitHubConsumerSecret = configurationProvider.GetSettingString("oauthGitHubConsumerSecret");
            _gitHubScopes = configurationProvider.GetSettingString("oauthGitHubScopes", false) ?? string.Empty;
            _gitHubRedirectUrl = configurationProvider.GetSettingString("oauthGitHubRedirectUrl", false);
        }

        public override void Dispose()
        {
            _configurationProvider.Dispose();
            _userRepository.Dispose();
            _connectorRepository.Dispose();
        }

        [Authenticate]
        public object Get(GitHubConnectorConnectRequest req)
        {
            string code = this.Request.QueryString.Get("code");

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
                    IAuthSession session = this.GetSession();
                    UserEntity user = _userRepository.Single(u => u.Id == Guid.Parse(session.UserAuthId));
                    ConnectorEntity connector = _connectorRepository.SingleOrDefault(c => c.Provider == Name && c.ProviderUserIdentifier == userProfile ["id"]);

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

        public static JsonObject GetUserProfile(string accessToken)
        {
            string userProfileUrl = UserProfileUrl.AddQueryParam("access_token", accessToken);
            var userProfileString = userProfileUrl.GetJsonFromUrl();
            var userProfile = JsonObject.Parse(userProfileString);

            return userProfile;
        }
    }

    [Route("/connect/github", "GET")]
    public class GitHubConnectorConnectRequest : IReturnVoid
    {
    }
}
