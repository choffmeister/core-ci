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
using System.Linq.Expressions;
using System.Collections;
using System.Linq;

namespace CoreCI.Models
{
    public class InMemoryRepository<TEntity> : IRepository<TEntity>
        where TEntity : class, IEntity, new()
    {
        private static readonly Dictionary<Guid, TEntity> _list = new Dictionary<Guid, TEntity>();

        public void Insert(TEntity model)
        {
            if (model.Id == default(Guid))
            {
                model.Id = Guid.NewGuid();
            }

            _list [model.Id] = model;
        }

        public void Insert(IEnumerable<TEntity> models)
        {
            foreach (TEntity model in models)
            {
                this.Insert(model);
            }
        }

        public void Update(TEntity model)
        {
            this.Insert(model);
        }

        public void InsertOrUpdate(TEntity model)
        {

            this.Insert(model);
        }

        public void Delete(TEntity model)
        {
            if (_list.ContainsKey(model.Id))
            {
                _list.Remove(model.Id);
            }
        }

        public void Delete(Guid id)
        {
            if (_list.ContainsKey(id))
            {
                _list.Remove(id);
            }
        }

        public void Clear()
        {
            _list.Clear();
        }

        public void Dispose()
        {
        }

        public IEnumerator<TEntity> GetEnumerator()
        {
            return _list.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public Type ElementType
        {
            get { return _list.Values.AsQueryable().ElementType; }
        }

        public Expression Expression
        {
            get { return _list.Values.AsQueryable().Expression; }
        }

        public IQueryProvider Provider
        {
            get { return _list.Values.AsQueryable().Provider; }
        }
    }
}
