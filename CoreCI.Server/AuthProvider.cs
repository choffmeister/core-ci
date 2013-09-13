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
using System.Linq;
using CoreCI.Models;
using Microsoft.Practices.Unity;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using System;

namespace CoreCI.Server
{
    public class AuthProvider : CredentialsAuthProvider
    {
        private readonly IUnityContainer _container;

        public AuthProvider(IUnityContainer container)
        {
            _container = container;
        }

        public override bool TryAuthenticate(IServiceBase authService, string userName, string password)
        {
            using (IUserRepository userRepository = _container.Resolve<IUserRepository>())
            {
                UserEntity user = userRepository.SingleOrDefault(u => u.UserName.ToLower() == userName.ToLower());

                if (user != null)
                {
                    return user.PasswordHash == password;
                }

                return false;
            }
        }

        public override void OnAuthenticated(IServiceBase authService, IAuthSession session, IOAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            using (IUserRepository userRepository = _container.Resolve<IUserRepository>())
            {
                UserEntity user = userRepository.Single(u => u.UserName.ToLower() == session.UserAuthName.ToLower());

                session.UserAuthId = user.Id.ToString();
                session.UserName = user.UserName;
                session.Email = user.Email;

                authService.SaveSession(session, TimeSpan.FromMinutes(30));
            }
        }
    }
}
