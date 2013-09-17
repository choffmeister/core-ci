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
    public interface IConnectorRepository : IRepository<ConnectorEntity>
    {
    }

    public class ConnectorRepository : MongoDbRepository<ConnectorEntity>, IConnectorRepository
    {
        protected ConnectorRepository(string connectionString, string collectionName)
            : base(connectionString, collectionName)
        {
        }

        public ConnectorRepository(IConfigurationProvider configurationProvider)
            : base(configurationProvider, "server.database", "connectors")
        {
        }

        public static ConnectorRepository CreateTemporary(string connectionString)
        {
            return new ConnectorRepository(connectionString, string.Format("{0}-{1}", "connectors", Guid.NewGuid()));
        }
    }
}
