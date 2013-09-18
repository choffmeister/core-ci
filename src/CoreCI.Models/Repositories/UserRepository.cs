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
using MongoDB.Driver.Builders;

namespace CoreCI.Models
{
    public interface IUserRepository : IRepository<UserEntity>
    {
    }

    public class UserRepository : MongoDbRepository<UserEntity>, IUserRepository
    {
        public UserRepository(IConfigurationProvider configurationProvider)
            : base(configurationProvider, "server.database", "users")
        {
            this.Configure();
        }

        protected UserRepository(string connectionString, string collectionName)
            : base(connectionString, collectionName)
        {
            this.Configure();
        }

        public static UserRepository CreateTemporary(string connectionString)
        {
            return new UserRepository(connectionString, string.Format("{0}-{1}", "users", Guid.NewGuid()));
        }

        private void Configure()
        {
            this.Collection.EnsureIndex(new IndexKeysBuilder().Ascending("UserName"), IndexOptions.SetUnique(true));
            this.Collection.EnsureIndex(new IndexKeysBuilder().Ascending("Email"), IndexOptions.SetUnique(true));
        }
    }
}
