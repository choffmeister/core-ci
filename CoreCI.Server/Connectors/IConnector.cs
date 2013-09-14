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
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Common.Web;
using System.Net;
using System.Collections.Generic;
using System.Security.Cryptography;
using CoreCI.Models;

namespace CoreCI.Server.Connectors
{
    public interface IConnector : IDisposable
    {
        object Connect(IAuthSession session, IHttpRequest request);

        object ProcessHook(IHttpRequest request);

        List<string> ListProjects(IAuthSession session, Guid connectorId);

        ProjectEntity AddProject(IAuthSession session, Guid connectorId, string projectName);

        void RemoveProject(IAuthSession session, Guid connectorId, Guid projectId);
    }

    public static class ConnectorExtensions
    {
        public static IHttpResult Redirect(this IConnector connector, string url, string message = null)
        {
            HttpResult httpResult = new HttpResult(HttpStatusCode.Found, message);
            httpResult.Headers.Add("Location", url);
            return httpResult;
        }

        public static string GenerateToken(this IConnector connector, int byteCount = 16)
        {
            var rng = RNGCryptoServiceProvider.Create();

            byte[] bytes = new byte[byteCount];
            rng.GetBytes(bytes);

            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }
}
