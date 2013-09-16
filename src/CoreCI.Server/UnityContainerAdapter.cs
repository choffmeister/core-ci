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
using ServiceStack.Configuration;
using Microsoft.Practices.Unity;

namespace CoreCI.Server
{
    /// <summary>
    /// IContainerAdapter for the unity framework.
    /// </summary>
    public class UnityContainerAdapter : IContainerAdapter
    {
        private readonly IUnityContainer _unityContainer;

        public UnityContainerAdapter(IUnityContainer container)
        {
            _unityContainer = container;
        }

        public T Resolve<T>()
        {
            return _unityContainer.Resolve<T>();
        }

        public T TryResolve<T>()
        {
            if (_unityContainer.IsRegistered(typeof(T)))
            {
                return (T)_unityContainer.Resolve(typeof(T));
            }

            return default(T);
        }
    }
}
