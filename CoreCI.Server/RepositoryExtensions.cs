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
using CoreCI.Models;
using ServiceStack.Common.Web;

namespace CoreCI.Server
{
    public static class RepositoryExtensions
    {
        public static TEntity GetEntityById<TEntity>(this IRepository<TEntity> repository, Guid id)
            where TEntity : class, IEntity, new()
        {
            TEntity entity = repository.SingleOrDefault(n => n.Id == id);

            if (entity == null)
            {
                throw HttpError.NotFound(string.Format("Could not find {1} with ID {0}", id, typeof(TEntity).Name));
            }

            return entity;
        }

        public static TEntity GetEntity<TEntity>(this IRepository<TEntity> repository, Func<TEntity, bool> predicate)
            where TEntity : class, IEntity, new()
        {
            TEntity entity = repository.SingleOrDefault(predicate);

            if (entity == null)
            {
                throw HttpError.NotFound(string.Format("Could not find {0}", typeof(TEntity).Name));
            }

            return entity;
        }
    }
}
