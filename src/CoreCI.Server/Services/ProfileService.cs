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
using CoreCI.Contracts;
using CoreCI.Models;
using ServiceStack.Common.Web;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;

namespace CoreCI.Server.Services
{
    public class ProfileService : Service
    {
        private readonly IUserRepository userRepository;
        private readonly IConnectorRepository connectorRepository;
        private readonly IProjectRepository projectRepository;

        public ProfileService(IUserRepository userRepository, IConnectorRepository connectorRepository, IProjectRepository projectRepository)
        {
            this.userRepository = userRepository;
            this.connectorRepository = connectorRepository;
            this.projectRepository = projectRepository;
        }

        public override void Dispose()
        {
            this.userRepository.Dispose();
            this.connectorRepository.Dispose();
            this.projectRepository.Dispose();
        }

        public ProfileRetrieveResponse Get(ProfileRetrieveRequest req)
        {
            IAuthSession session = this.GetSession();

            if (!string.IsNullOrEmpty(req.UserName))
            {
                UserEntity user = this.userRepository
                    .GetEntity(u => u.UserName == req.UserName.ToLower())
                    .CloneWithoutSecrets();

                return new ProfileRetrieveResponse()
                {
                    User = user
                };
            }
            else if (!string.IsNullOrEmpty(session.UserAuthName))
            {
                UserEntity user = this.userRepository
                    .Single(u => u.UserName == session.UserAuthName)
                    .CloneWithoutSecrets();
                List<ConnectorEntity> connectors = this.connectorRepository
                    .Where(c => c.UserId == user.Id)
                    .Select(c => c.CloneWithoutSecrets())
                    .ToList();
                List<ProjectEntity> projects = this.projectRepository
                    .Where(p => p.UserId == user.Id)
                    .Select(p => p.CloneWithoutSecrets())
                    .ToList();

                return new ProfileRetrieveResponse()
                {
                    User = user,
                    Connectors = connectors,
                    Projects = projects
                };
            }

            throw HttpError.NotFound("Either specify a user name or log in to view your own profile");
        }
    }
}
