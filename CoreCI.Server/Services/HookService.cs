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
using CoreCI.Contracts;
using CoreCI.Models;
using NLog;
using ServiceStack.Common.Web;
using ServiceStack.ServiceInterface;
using ServiceStack.Text;
using CoreCI.Server.Hooks;
using System.Reflection;
using ServiceStack.ServiceHost;
using Microsoft.Practices.Unity;

namespace CoreCI.Server.Services
{
    /// <summary>
    /// Service that allows other applications like GitHub
    /// to invoke tasks.
    /// </summary>
    public class HookService : Service
    {
        private readonly static Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IUnityContainer _container;
        private readonly ITaskRepository _taskRepository;

        public HookService(IUnityContainer container, ITaskRepository taskRepository)
        {
            _container = container;
            _taskRepository = taskRepository;
        }

        public override void Dispose()
        {
            _taskRepository.Dispose();
        }

        public HookResponse Post(HookRequest req)
        {
            _logger.Trace("Received hook for {0}", req.HookName);

            var hookDescriptor = typeof(IHook).Assembly.GetTypes()
                .Where(t => typeof(IHook).IsAssignableFrom(t))
                .Select(t => new {
                    HookType = t,
                    HookAttribute = t.GetCustomAttributes(typeof(HookAttribute), false).SingleOrDefault() as HookAttribute
                })
                .Where(hd => hd.HookAttribute != null)
                .SingleOrDefault(hd => hd.HookAttribute.Name == req.HookName);

            if (hookDescriptor == null)
            {
                throw HttpError.NotFound(string.Format("Unknown hook {0}", req.HookName));
            }

            using (IHook hook = (IHook)_container.Resolve(hookDescriptor.HookType))
            {
                hook.Process(this.Request);
            }

            return new HookResponse();
        }
    }
}
