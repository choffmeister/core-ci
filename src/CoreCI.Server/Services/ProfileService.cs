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
using CoreCI.Models;
using CoreCI.Contracts;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Common.Web;
using System.Collections.Generic;
using System.Net;

namespace CoreCI.Server.Services
{
    public class ProfileService : Service
    {
        private readonly IUserRepository _userRepository;
        private readonly IConnectorRepository _connectorRepository;
        private readonly IProjectRepository _projectRepository;

        public ProfileService(IUserRepository userRepository, IConnectorRepository connectorRepository, IProjectRepository projectRepository)
        {
            _userRepository = userRepository;
            _connectorRepository = connectorRepository;
            _projectRepository = projectRepository;
        }

        public override void Dispose()
        {
            _userRepository.Dispose();
            _connectorRepository.Dispose();
            _projectRepository.Dispose();
        }

        public ProfileRetrieveResponse Get(ProfileRetrieveRequest req)
        {
            IAuthSession session = this.GetSession();

            if (!string.IsNullOrEmpty(req.UserName))
            {
                UserEntity user = _userRepository.GetEntity(u => u.UserName == req.UserName.ToLower());

                StripSecrets(user);

                return new ProfileRetrieveResponse()
                {
                    User = user
                };
            }
            else if (!string.IsNullOrEmpty(session.UserAuthName))
            {
                UserEntity user = _userRepository.Single(u => u.UserName == session.UserAuthName);
                List<ConnectorEntity> connectors = _connectorRepository.Where(c => c.UserId == user.Id).ToList();
                List<ProjectEntity> projects = _projectRepository.Where(p => p.UserId == user.Id).ToList();

                StripSecrets(user);

                return new ProfileRetrieveResponse()
                {
                    User = user,
                    Connectors = connectors,
                    Projects = projects
                };
            }

            throw HttpError.NotFound("Either specify a user name or log in to view your own profile");
        }

        private static void StripSecrets(UserEntity user)
        {
            user.PasswordHash = null;
            user.PasswordHashAlgorithm = null;
            user.PasswordSalt = null;
        }
    }
}
