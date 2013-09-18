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
using System.Linq;
using System.Net;
using System.Text;
using ServiceStack.Text;

namespace CoreCI.Server.Connectors
{
    public static class GitHubOAuth2Client
    {
        public const string AuthorizeUrl = "https://github.com/login/oauth/authorize?client_id={0}&scope={1}";
        public const string AccessTokenUrl = "https://github.com/login/oauth/access_token?client_id={0}&client_secret={1}&code={2}";
        public const string UserProfileUrl = "https://api.github.com/user?access_token={0}";
        public const string RepositoriesUrl = "https://api.github.com/user/repos?access_token={0}";
        public const string GetContentUrl = "https://api.github.com/repos/{1}/{2}/contents/{3}?access_token={0}&ref={4}";
        public const string CreateHookUrl = "https://api.github.com/repos/{1}/{2}/hooks?access_token={0}";
        public const string ListHooksUrl = "https://api.github.com/repos/{1}/{2}/hooks?access_token={0}";
        public const string DeleteHookUrl = "https://api.github.com/repos/{1}/{2}/hooks/{3}?access_token={0}";
        public const string CreateKeyUrl = "https://api.github.com/repos/{1}/{2}/keys?access_token={0}";
        public const string ListKeysUrl = "https://api.github.com/repos/{1}/{2}/keys?access_token={0}";
        public const string DeleteKeyUrl = "https://api.github.com/repos/{1}/{2}/keys/{3}?access_token={0}";

        public static string GetAuthorizeUrl(string clientId, string scopes)
        {
            string authorizeUrl = string.Format(AuthorizeUrl, clientId, scopes);

            return authorizeUrl;
        }

        public static string GetAccessToken(string clientId, string clientSecret, string code)
        {
            string accessTokenUrl = string.Format(AccessTokenUrl, clientId, clientSecret, code);

            var response = JsonObject.Parse(accessTokenUrl.GetJsonFromUrl());

            return response["access_token"];
        }

        public static JsonArrayObjects GetRepositories(string accessToken)
        {
            string repositoryUrl = string.Format(RepositoriesUrl, accessToken);

            var repositories = JsonArrayObjects.Parse(repositoryUrl.GetJsonFromUrl());

            return repositories;
        }

        public static JsonObject GetUserProfile(string accessToken)
        {
            string userProfileUrl = string.Format(UserProfileUrl, accessToken);

            var userProfile = JsonObject.Parse(userProfileUrl.GetJsonFromUrl());

            return userProfile;
        }

        public static string GetContent(string accessToken, string ownerName, string repositoryName, string path, string reference)
        {
            try
            {
                string getContentUrl = string.Format(GetContentUrl, accessToken, ownerName, repositoryName, path, reference);

                var content = JsonObject.Parse(getContentUrl.GetJsonFromUrl());
                string encoding = content.Get("encoding");
                string encodedContent = content.Get("content");

                switch (encoding)
                {
                    case "base64":
                        string base64 = encodedContent.Replace("\n", string.Empty);

                        return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                    default:
                        throw new NotSupportedException(string.Format("Encoding '{0}' is not supported", encoding));
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

                throw ex;
            }
        }

        public static void RemoveHooks(string accessToken, string ownerName, string repositoryName, Func<JsonObject, bool> predicate)
        {
            string listHooksUrl = string.Format(ListHooksUrl, accessToken, ownerName, repositoryName);

            var hooks = JsonArrayObjects.Parse(listHooksUrl.GetJsonFromUrl());

            foreach (var hook in hooks.Where(predicate))
            {
                string deleteHookUrl = string.Format(DeleteHookUrl, accessToken, ownerName, repositoryName, hook.Child("id"));

                deleteHookUrl.DeleteFromUrl();
            }
        }

        public static void CreateHook(string accessToken, string ownerName, string repositoryName, string url)
        {
            string createHookUrl = string.Format(CreateHookUrl, accessToken, ownerName, repositoryName);

            createHookUrl.PostJsonToUrl(new
            {
                name = "web",
                active = true,
                events = new List<string>() { "push" },
                config = new Dictionary<string, string>() { { "url", url } },
            });
        }

        public static void RemoveKeys(string accessToken, string ownerName, string repositoryName, Func<JsonObject, bool> predicate)
        {
            string listKeysUrl = string.Format(ListKeysUrl, accessToken, ownerName, repositoryName);

            var keys = JsonArrayObjects.Parse(listKeysUrl.GetJsonFromUrl());

            foreach (var key in keys.Where(predicate))
            {
                string deleteKeyUrl = string.Format(DeleteKeyUrl, accessToken, ownerName, repositoryName, key.Child("id"));

                deleteKeyUrl.DeleteFromUrl();
            }
        }

        public static void CreateKey(string accessToken, string ownerName, string repositoryName, string publicKeyString)
        {
            string createKeyUrl = string.Format(CreateKeyUrl, accessToken, ownerName, repositoryName);

            createKeyUrl.PostJsonToUrl(new
            {
                key = publicKeyString
            });
        }
    }
}
