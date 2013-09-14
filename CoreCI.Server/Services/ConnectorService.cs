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
using Microsoft.Practices.Unity;
using NLog;
using CoreCI.Models;
using CoreCI.Contracts;
using ServiceStack.Common.Web;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.ServiceHost;

namespace CoreCI.Server.Services
{
    public class ConnectorService : Service
    {
        private readonly IUnityContainer _container;

        public ConnectorService(IUnityContainer container)
        {
            _container = container;
        }

        [Authenticate]
        public object Get(ConnectorConnectRequest req)
        {
            var connectorDescriptor = typeof(IConnector).Assembly.GetTypes()
                .Where(t => typeof(IConnector).IsAssignableFrom(t))
                .Select(t => new {
                    ConnectorType = t,
                    ConnectorAttribute = t.GetCustomAttributes(typeof(ConnectorAttribute), false).SingleOrDefault() as ConnectorAttribute
                })
                .Where(hd => hd.ConnectorAttribute != null)
                .SingleOrDefault(hd => hd.ConnectorAttribute.Name == req.ConnectorName);

            if (connectorDescriptor == null)
            {
                throw HttpError.NotFound(string.Format("Unknown connector {0}", req.ConnectorName));
            }

            using (IConnector connector = (IConnector)_container.Resolve(connectorDescriptor.ConnectorType))
            {
                IAuthSession session = this.GetSession();
                IHttpRequest request = this.Request;

                object result = connector.Connect(session, request);

                if (result != null)
                {
                    return result;
                }

                return new ConnectorConnectResponse();
            }
        }
    }
}
