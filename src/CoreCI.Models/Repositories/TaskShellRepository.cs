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
    public interface ITaskShellRepository : IRepository<TaskShellEntity>
    {
    }

    public class TaskShellRepository : MongoDbRepository<TaskShellEntity>, ITaskShellRepository
    {
        protected TaskShellRepository(string connectionString, string collectionName)
            : base(connectionString, collectionName)
        {
        }

        public TaskShellRepository(IConfigurationProvider configurationProvider)
            : base(configurationProvider, "server.database", "taskshells")
        {
        }

        public static TaskShellRepository CreateTemporary(string connectionString)
        {
            return new TaskShellRepository(connectionString, string.Format("{0}-{1}", "taskshells", Guid.NewGuid()));
        }
    }
}
