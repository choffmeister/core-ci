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
using CoreCI.Server.Connectors;

namespace CoreCI.Server.Services
{
    public class ConnectorService : Service
    {
        private readonly IUnityContainer _container;
        private readonly IConnectorRepository _connectorRepository;

        public ConnectorService(IUnityContainer container, IConnectorRepository connectorRepository)
        {
            _container = container;
            _connectorRepository = connectorRepository;
        }

        public override void Dispose()
        {
            _connectorRepository.Dispose();
        }

        [Authenticate]
        public object Get(ConnectorConnectRequest req)
        {
            ConnectorDescriptor desc = GetConnectorDescriptor(req.ConnectorName);

            using (IConnector connector = (IConnector)_container.Resolve(desc.Type))
            {
                IAuthSession session = this.GetSession();
                IHttpRequest request = this.Request;

                return connector.Connect(session, request);
            }
        }

        public object Post(ConnectorProcessHookRequest req)
        {
            ConnectorDescriptor desc = GetConnectorDescriptor(req.ConnectorName);

            using (IConnector connector = (IConnector)_container.Resolve(desc.Type))
            {
                IHttpRequest request = this.Request;

                return connector.ProcessHook(request);
            }
        }

        [Authenticate]
        public ConnectorListProjectsResponse Get(ConnectorListProjectsRequest req)
        {
            ConnectorDescriptor desc = GetConnectorDescriptor(req.ConnectorId);

            using (IConnector connector = (IConnector)_container.Resolve(desc.Type))
            {
                IAuthSession session = this.GetSession();

                return new ConnectorListProjectsResponse()
                {
                    Projects = connector.ListProjects(session, req.ConnectorId)
                };
            }
        }

        [Authenticate]
        public ConnectorAddProjectResponse Post(ConnectorAddProjectRequest req)
        {
            ConnectorDescriptor desc = GetConnectorDescriptor(req.ConnectorId);

            using (IConnector connector = (IConnector)_container.Resolve(desc.Type))
            {
                IAuthSession session = this.GetSession();

                ProjectEntity project = connector.AddProject(session, req.ConnectorId, req.ProjectName);

                PushService.Push("projects", null);
                PushService.Push("project-" + project.Id.ToString().Replace("-", "").ToLowerInvariant(), "created");

                return new ConnectorAddProjectResponse();
            }
        }

        [Authenticate]
        public ConnectorRemoveProjectResponse Delete(ConnectorRemoveProjectRequest req)
        {
            ConnectorDescriptor desc = GetConnectorDescriptor(req.ConnectorId);

            using (IConnector connector = (IConnector)_container.Resolve(desc.Type))
            {
                IAuthSession session = this.GetSession();

                connector.RemoveProject(session, req.ConnectorId, req.ProjectId);

                PushService.Push("projects", null);
                PushService.Push("project-" + req.ProjectId.ToString().Replace("-", "").ToLowerInvariant(), "removed");

                return new ConnectorRemoveProjectResponse();
            }
        }

        private ConnectorDescriptor GetConnectorDescriptor(Guid connectorId)
        {
            string name = _connectorRepository.Single(c => c.Id == connectorId).Provider;
            ConnectorDescriptor connectorDescriptor = ConnectorDescriptor.GetByName(name);

            if (connectorDescriptor == null)
            {
                throw HttpError.NotFound(string.Format("Unknown connector {0}", connectorId));
            }

            return connectorDescriptor;
        }

        private ConnectorDescriptor GetConnectorDescriptor(string connectorName)
        {
            ConnectorDescriptor connectorDescriptor = ConnectorDescriptor.GetByName(connectorName);

            if (connectorDescriptor == null)
            {
                throw HttpError.NotFound(string.Format("Unknown connector {0}", connectorName));
            }

            return connectorDescriptor;
        }
    }

    internal class ConnectorDescriptor
    {
        public Type Type { get; set; }

        public ConnectorAttribute Meta { get; set; }

        public ConnectorDescriptor(Type type)
        {
            this.Type = type;
            this.Meta = type.GetCustomAttributes(typeof(ConnectorAttribute), false).SingleOrDefault() as ConnectorAttribute;
        }

        public static ConnectorDescriptor GetByName(string name)
        {
            return typeof(IConnector).Assembly.GetTypes()
                .Where(t => typeof(IConnector).IsAssignableFrom(t))
                .Select(t => new ConnectorDescriptor(t))
                .Where(hd => hd.Meta != null)
                .SingleOrDefault(hd => hd.Meta.Name == name);
        }
    }
}
