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
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using CoreCI.Common;

namespace CoreCI.Models
{
    /// <summary>
    /// A implementation of the <see cref="IRepository<T>"/> interface for MongoDB
    /// databases. Allows easy using (see example) with little code.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <example>
    /// <code>
    /// public class MyEntity : IEntity
    /// {
    ///     public Guid Id { get; set; }
    ///     public string Name { get; set; }
    /// }
    ///
    /// public class MyEntityRepository : MongoDbRepository<MyEntity>, IRepository<MyEntity>
    /// {
    ///     public MyEntityRepository(IConfigurationProvider configurationProvider)
    ///         : base(configurationProvider, "connectionStringName", "myentities")
    ///     {
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class MongoDbRepository<TEntity> : IRepository<TEntity>
        where TEntity : class, IEntity, new()
    {
        private static readonly Regex _connectionStringRegex = new Regex("^Server=(?<Server>[^;]+);Database=(?<Database>[^;]+)(;Username=(?<Username>[^;]+))?(;Password=(?<Password>.+))?$");
        private readonly MongoClient _client;
        private readonly MongoServer _server;
        private readonly MongoDatabase _database;
        private readonly MongoCollection<TEntity> _collection;
        private readonly IQueryable<TEntity> _queryable;

        /// <summary>
        /// Gets the collection. Grants sub classes direct access to the MongoDB specific functionalities.
        /// </summary>
        /// <value>
        /// The collection.
        /// </value>
        protected MongoCollection<TEntity> Collection
        {
            get { return _collection; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbRepository{TEntity}"/> class.
        /// The connection string is requested from the configuration provider.
        /// </summary>
        /// <param name="configurationProvider">The configuration provider.</param>
        /// <param name="connectionStringName">Name of the connection string.</param>
        /// <param name="collectionName">Name of the collection.</param>
        /// <exception cref="System.NotImplementedException">MongoDB connection with credentials has not been implemented yet</exception>
        /// <exception cref="System.Exception"></exception>
        public MongoDbRepository(IConfigurationProvider configurationProvider, string connectionStringName, string collectionName)
        {
            // read connection string
            string connectionString = configurationProvider.GetConnectionString(connectionStringName);

            // split connection string by regex
            Match match = _connectionStringRegex.Match(connectionString);

            if (match.Success)
            {
                // connection string is valid, so extract the important parts
                string server = match.Groups ["Server"].Value;
                string database = match.Groups ["Database"].Value;
                string username = match.Groups ["Username"].Value;
                string password = match.Groups ["Password"].Value;

                // create MongoDB client
                _client = new MongoClient(server);
                _server = _client.GetServer();

                if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
                {
                    throw new NotImplementedException("MongoDB connection with credentials has not been implemented yet");
                }
                else
                {
                    _database = _server.GetDatabase(database);
                }

                // connect to collection defined by collectionName parameter
                _collection = _database.GetCollection<TEntity>(collectionName);
                _queryable = _collection.AsQueryable();
            }
            else
            {
                // connection string is invalid
                throw new Exception(string.Format("Cannot parse connection string '{0}'", connectionString));
            }
        }

        /// <summary>
        /// Does nothing, since explicit disposing of database connections is not needed
        /// with MongoDB. Furthermore explicit disposing causes sporadic database connection
        /// errors (see <see cref="!:https://jira.mongodb.org/browse/CSHARP-738"/>).
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Inserts the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        public void Insert(TEntity model)
        {
            if (model.Id == default(Guid))
            {
                model.Id = Guid.NewGuid();
            }

            _collection.Insert(model);
        }

        /// <summary>
        /// Inserts the specified models.
        /// </summary>
        /// <param name="models">The models.</param>
        public void Insert(IEnumerable<TEntity> models)
        {
            foreach (TEntity model in models)
            {
                if (model.Id == default(Guid))
                {
                    model.Id = Guid.NewGuid();
                }
            }

            _collection.InsertBatch(models);
        }

        /// <summary>
        /// Updates the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        public void Update(TEntity model)
        {
            _collection.Save(model);
        }

        /// <summary>
        /// Inserts or updates the model.
        /// </summary>
        /// <param name="model">The model.</param>
        public void InsertOrUpdate(TEntity model)
        {
            if (model.Id == default(Guid))
            {
                model.Id = Guid.NewGuid();

                this.Insert(model);
            }
            else
            {
                this.Update(model);
            }
        }

        /// <summary>
        /// Deletes the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        public void Delete(TEntity model)
        {
            _collection.Remove(Query.EQ("_id", model.Id));
        }

        /// <summary>
        /// Deletes a model be the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        public void Delete(Guid id)
        {
            _collection.Remove(Query.EQ("_id", id));
        }

        /// <summary>
        /// Clears the database.
        /// </summary>
        public void Clear()
        {
            _collection.Drop();
        }
        #region IQueryable, IEnumerable
        public IEnumerator<TEntity> GetEnumerator()
        {
            return _queryable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _queryable.GetEnumerator();
        }

        public Type ElementType
        {
            get { return _queryable.ElementType; }
        }

        public Expression Expression
        {
            get { return _queryable.Expression; }
        }

        public IQueryProvider Provider
        {
            get { return _queryable.Provider; }
        }
        #endregion IQueryable, IEnumerable
    }
}
