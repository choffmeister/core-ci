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
using CoreCI.Common;
using CoreCI.Models;
using Funq;
using Microsoft.Practices.Unity;
using NLog;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

namespace CoreCI.Server
{
    public class ServerHandler : AppHostHttpListenerBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly IUnityContainer unityContainer;
        private readonly IConfigurationProvider configurationProvider;

        public ServerHandler(IConfigurationProvider configurationProvider)
            : base("core:ci", typeof(ServerHandler).Assembly)
        {
            this.unityContainer = new UnityContainer();
            this.configurationProvider = configurationProvider;

            this.ServiceExceptionHandler += (req, ex) =>
            {
                // HTTP exceptions are only logged to trace
                if (ex is IHttpError == false)
                {
                    Log.Error(ex);
                }
                else
                {
                    Log.Trace(ex);
                }

                // return with default exception handling
                return DtoUtils.HandleException(this, req, ex);
            };
            this.ExceptionHandler += (req, res, operationName, ex) =>
            {
                Log.Error(ex);
            };
        }

        public IUnityContainer UnityContainer
        {
            get { return this.unityContainer; }
        }

        public void Start()
        {
            string baseAddress = this.configurationProvider.Get("server.addresses.internal.api");

            Log.Info("Start listening on {0}", baseAddress);
            this.Start(baseAddress);
            Log.Info("Started");
        }

        public override void Stop()
        {
            Log.Info("Stop listening");
            base.Stop();
            Log.Info("Stopped");
        }

        public override void Configure(Container container)
        {
            JsConfig.AlwaysUseUtc = true;
            JsConfig.DateHandler = JsonDateHandler.ISO8601;
            JsConfig.EmitCamelCaseNames = true;

            this.unityContainer
                .RegisterInstance<IConfigurationProvider>(this.configurationProvider)
                .RegisterType<IConnectorRepository, ConnectorRepository>()
                .RegisterType<IUserRepository, UserRepository>()
                .RegisterType<IWorkerRepository, WorkerRepository>()
                .RegisterType<IProjectRepository, ProjectRepository>()
                .RegisterType<ITaskRepository, TaskRepository>()
                .RegisterType<ITaskShellRepository, TaskShellRepository>();

            container.Adapter = new UnityContainerAdapter(this.unityContainer);

            List<IAuthProvider> authProviders = new List<IAuthProvider>();
            authProviders.Add(new AuthProvider(this.unityContainer));

            IPlugin authFeature = new AuthFeature(() => new AuthUserSession(), authProviders.ToArray());
            this.Plugins.Add(authFeature);
        }
    }
}
