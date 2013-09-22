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
using System.Linq;
using CoreCI.Contracts;
using CoreCI.Models;
using ServiceStack.ServiceInterface;

namespace CoreCI.Server.Services
{
    public class ProjectService : Service
    {
        private readonly IProjectRepository projectRepository;

        public ProjectService(IProjectRepository projectRepository)
        {
            this.projectRepository = projectRepository;
        }

        public override void Dispose()
        {
            this.projectRepository.Dispose();
        }

        public ProjectListResponse Get(ProjectListRequest req)
        {
            return new ProjectListResponse()
            {
                Projects = this.projectRepository
                    .OrderBy(p => p.Name)
                    .Select(p => p.CloneWithoutSecrets())
                    .ToList()
            };
        }

        public ProjectRetrieveResponse Get(ProjectRetrieveRequest req)
        {
            ProjectEntity project = this.projectRepository.GetEntityById(req.ProjectId).CloneWithoutSecrets();

            return new ProjectRetrieveResponse()
            {
                Project = project
            };
        }
    }
}
