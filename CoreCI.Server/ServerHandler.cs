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

namespace CoreCI.Server
{
    public class ServerHandler : AppHostHttpListenerBase
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IConfigurationProvider _configurationProvider;

        public ServerHandler(IConfigurationProvider configurationProvider)
            : base("core:ci", typeof(ServerHandler).Assembly)
        {
            _configurationProvider = configurationProvider;

            this.ServiceExceptionHandler += (req, ex) =>
            {
                _logger.Error(ex);
                return null;
            };
            this.ExceptionHandler += (httpReq, httpRes, operationName, ex) =>
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

            container.Register<IConfigurationProvider>(_configurationProvider);
            container.RegisterAs<WorkerRepository, IWorkerRepository>().ReusedWithin(ReuseScope.None);
            container.RegisterAs<TaskRepository, ITaskRepository>().ReusedWithin(ReuseScope.None);
            container.RegisterAs<TaskShellRepository, ITaskShellRepository>().ReusedWithin(ReuseScope.None);
        }
    }
}
