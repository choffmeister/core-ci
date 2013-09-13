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
using ServiceStack.WebHost.Endpoints;
using Funq;
using CoreCI.Models;
using ServiceStack.Text;
using CoreCI.Common;
using NLog;
using ServiceStack.ServiceHost;
using System.Net;
using Microsoft.Practices.Unity;

namespace CoreCI.Server
{
    public class ServerHandler : AppHostHttpListenerBase
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IUnityContainer _unityContainer;
        private readonly IConfigurationProvider _configurationProvider;

        public IUnityContainer UnityContainer
        {
            get { return _unityContainer; }
        }

        public ServerHandler(IConfigurationProvider configurationProvider)
            : base("core:ci", typeof(ServerHandler).Assembly)
        {
            _unityContainer = new UnityContainer();
            _configurationProvider = configurationProvider;

            this.ServiceExceptionHandler += (req, ex) =>
            {
                // HTTP exceptions are only logged to trace
                if (ex is IHttpError == false)
                {
                    _logger.Error(ex);
                }
                else
                {
                    _logger.Trace(ex);
                }

                // return with default exception handling
                return DtoUtils.HandleException(this, req, ex);
            };
            this.ExceptionHandler += (req, res, operationName, ex) =>
            {
                _logger.Error(ex);
            };
        }

        public void Start()
        {
            string baseAddress = _configurationProvider.GetSettingString("serverApiBaseAddress");

            _logger.Info("Start listening on {0}", baseAddress);
            this.Start(baseAddress);
            _logger.Info("Started");
        }

        public override void Stop()
        {
            _logger.Info("Stop listening");
            base.Stop();
            _logger.Info("Stopped");
        }

        public override void Configure(Container container)
        {
            JsConfig.AlwaysUseUtc = true;
            JsConfig.DateHandler = JsonDateHandler.ISO8601;
            JsConfig.EmitCamelCaseNames = true;

            _unityContainer
                .RegisterInstance<IConfigurationProvider>(_configurationProvider)
                .RegisterType<IWorkerRepository, WorkerRepository>()
                .RegisterType<IProjectRepository, ProjectRepository>()
                .RegisterType<ITaskRepository, TaskRepository>()
                .RegisterType<ITaskShellRepository, TaskShellRepository>();

            container.Adapter = new UnityContainerAdapter(_unityContainer);
        }
    }
}
