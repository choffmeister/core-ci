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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using CoreCI.Common;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;

namespace CoreCI.Models
{
    /// <summary>
    /// A implementation of the <see cref="IRepository{T}"/> interface for MongoDB
    /// databases. Allows easy using (see example) with little code.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <example>
    /// <code>
    /// public class MyEntity : IEntity
    /// {
    ///     public Guid Id { get; set; }
    ///     public string Name { get; set; }
    /// }
    ///
    /// public class MyEntityRepository : MongoDbRepository&lt;MyEntity&gt;, IRepository&lt;MyEntity&gt;
    /// {
    ///     public MyEntityRepository(IConfigurationProvider configurationProvider)
    ///         : base(configurationProvider, "connectionStringName", "myentities")
    ///     {
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract partial class MongoDbRepository<TEntity> : IRepository<TEntity>
        where TEntity : class, IEntity, new()
    {
        private static readonly Regex ConnectionStringRegex = new Regex("^Server=(?<Server>[^;]+);Database=(?<Database>[^;]+)(;Username=(?<Username>[^;]+))?(;Password=(?<Password>.+))?$");
        private readonly MongoClient client;
        private readonly MongoServer server;
        private readonly MongoDatabase database;
        private readonly MongoCollection<TEntity> collection;
        private readonly IQueryable<TEntity> queryable;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbRepository{TEntity}"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="collectionName">Name of the collection.</param>
        /// <exception cref="System.NotImplementedException">MongoDB connection with credentials has not been implemented yet</exception>
        /// <exception cref="System.Exception"></exception>
        public MongoDbRepository(string connectionString, string collectionName)
        {
            // split connection string by regex
            Match match = ConnectionStringRegex.Match(connectionString);

            if (match.Success)
            {
                // connection string is valid, so extract the important parts
                string server = match.Groups["Server"].Value;
                string database = match.Groups["Database"].Value;
                string username = match.Groups["Username"].Value;
                string password = match.Groups["Password"].Value;

                // create MongoDB client
                this.client = new MongoClient(server);
                this.server = this.client.GetServer();

                if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
                {
                    throw new NotImplementedException("MongoDB connection with credentials has not been implemented yet");
                }
                else
                {
                    this.database = this.server.GetDatabase(database);
                }

                // connect to collection defined by collectionName parameter
                this.collection = this.database.GetCollection<TEntity>(collectionName);
                this.queryable = this.collection.AsQueryable();
            }
            else
            {
                // connection string is invalid
                throw new Exception(string.Format("Cannot parse connection string '{0}'", connectionString));
            }
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
            : this(configurationProvider.Get(connectionStringName), collectionName)
        {
        }

        /// <summary>
        /// Gets the collection. Grants sub classes direct access to the MongoDB specific functionalities.
        /// </summary>
        /// <value>
        /// The collection.
        /// </value>
        protected MongoCollection<TEntity> Collection
        {
            get { return this.collection; }
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

            this.collection.Insert(model);
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

            this.collection.InsertBatch(models);
        }

        /// <summary>
        /// Updates the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        public void Update(TEntity model)
        {
            this.collection.Save(model);
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
            this.collection.Remove(Query.EQ("_id", model.Id));
        }

        /// <summary>
        /// Deletes a model be the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        public void Delete(Guid id)
        {
            this.collection.Remove(Query.EQ("_id", id));
        }

        /// <summary>
        /// Clears the database.
        /// </summary>
        public void Clear()
        {
            this.collection.Drop();
        }
    }

    /// <summary>
    /// A implementation of the <see cref="IRepository{T}"/> interface for MongoDB
    /// databases. Allows easy using (see example) with little code.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <example>
    /// <code>
    /// public class MyEntity : IEntity
    /// {
    ///     public Guid Id { get; set; }
    ///     public string Name { get; set; }
    /// }
    ///
    /// public class MyEntityRepository : MongoDbRepository&lt;MyEntity&gt;, IRepository&lt;MyEntity&gt;
    /// {
    ///     public MyEntityRepository(IConfigurationProvider configurationProvider)
    ///         : base(configurationProvider, "connectionStringName", "myentities")
    ///     {
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract partial class MongoDbRepository<TEntity> : IQueryable<TEntity>
    {
        public Type ElementType
        {
            get { return this.queryable.ElementType; }
        }

        public Expression Expression
        {
            get { return this.queryable.Expression; }
        }

        public IQueryProvider Provider
        {
            get { return this.queryable.Provider; }
        }

        public IEnumerator<TEntity> GetEnumerator()
        {
            return this.queryable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.queryable.GetEnumerator();
        }
    }
}
