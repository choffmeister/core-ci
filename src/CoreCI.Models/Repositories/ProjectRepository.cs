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
using CoreCI.Common;

namespace CoreCI.Models
{
    public interface IProjectRepository : IRepository<ProjectEntity>
    {
    }

    public class ProjectRepository : MongoDbRepository<ProjectEntity>, IProjectRepository
    {
        public ProjectRepository(IConfigurationProvider configurationProvider)
            : base(configurationProvider, "server.database", "projects")
        {
        }

        protected ProjectRepository(string connectionString, string collectionName)
            : base(connectionString, collectionName)
        {
        }

        public static ProjectRepository CreateTemporary(string connectionString)
        {
            return new ProjectRepository(connectionString, string.Format("{0}-{1}", "projects", Guid.NewGuid()));
        }
    }
}
