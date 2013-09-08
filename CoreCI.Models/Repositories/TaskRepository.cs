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
using CoreCI.Common;
using MongoDB.Driver.Builders;

namespace CoreCI.Models
{
    public interface ITaskRepository : IRepository<TaskEntity>
    {
        TaskEntity GetPendingTask(Guid workerId);
    }

    public class TaskRepository : MongoDbRepository<TaskEntity>, ITaskRepository
    {
        protected TaskRepository(string connectionString, string collectionName)
            : base(connectionString, collectionName)
        {
        }

        public TaskRepository(IConfigurationProvider configurationProvider)
            : base(configurationProvider, "coreciDatabase", "tasks")
        {
        }

        public static TaskRepository CreateTemporary(string connectionString)
        {
            return new TaskRepository(connectionString, string.Format("{0}-{1}", "tasks", Guid.NewGuid()));
        }

        public TaskEntity GetPendingTask(Guid workerId)
        {
            var query = Query<TaskEntity>.EQ(t => t.State, TaskState.Pending);
            var sortBy = SortBy<TaskEntity>.Ascending(t => t.CreatedAt);
            var update = Update<TaskEntity>
                .Set(t => t.State, TaskState.Running)
                .Set(t => t.DispatchedAt, DateTime.UtcNow)
                .Set(t => t.WorkerId, workerId);

            var result = this.Collection.FindAndModify(query, sortBy, update, true);

            return result.GetModifiedDocumentAs<TaskEntity>();
        }
    }
}
