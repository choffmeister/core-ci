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
using System.Collections.Generic;
using System.Linq;

namespace CoreCI.Models
{
    /// <summary>
    /// Base interface for data access repositories.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    public interface IRepository<TEntity> : IQueryable<TEntity>, IDisposable
        where TEntity : class, IEntity, new()
    {
        /// <summary>
        /// Inserts an new entity. Creates a GUID id if not already set.
        /// </summary>
        /// <param name="model">The entity to add.</param>
        void Insert(TEntity model);

        /// <summary>
        /// Inserts multiple new entites. Creates a GUID id for each if not already set.
        /// </summary>
        /// <param name="models">The entities to add.</param>
        void Insert(IEnumerable<TEntity> models);

        /// <summary>
        /// Updates an entity.
        /// </summary>
        /// <param name="model">The entity to update</param>
        void Update(TEntity model);

        /// <summary>
        /// Inserts or updates a new entity. Which path to take is decided by the presence
        /// or absence of the GUID id of the entity.
        /// </summary>
        /// <param name="model">The entity.</param>
        void InsertOrUpdate(TEntity model);

        /// <summary>
        /// Deletes an entity.
        /// </summary>
        /// <param name="model">The entity to delete.</param>
        void Delete(TEntity model);

        /// <summary>
        /// Deletes an entity by its id.
        /// </summary>
        /// <param name="id">The entity id.</param>
        void Delete(Guid id);

        /// <summary>
        /// Clears the whole dataset.
        /// </summary>
        void Clear();
    }
}
