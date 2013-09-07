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
using NUnit.Framework;
using CoreCI.Models;

namespace CoreCI.Tests.Models
{
    [TestFixture]
    public class MongoDbRepositoryTest
    {
        private int _i = 0;
        private IRepository<TestEntity> _repo;

        [SetUpAttribute]
        public void SetUp()
        {
            _repo = CreateRepository("Server=mongodb://localhost;Database=coreci-test");
        }

        [TearDownAttribute]
        public void TearDown()
        {
            _repo.Clear();
            _repo.Dispose();
        }

        [Test]
        public void TestInserting()
        {
            TestEntity e1 = CreateEntity(_i++);
            TestEntity e2 = CreateEntity(_i++);
            TestEntity e3 = CreateEntity(_i++);

            Assert.AreEqual(0, _repo.Count());

            _repo.Insert(e1);

            Assert.AreEqual(1, _repo.Count());
            Assert.AreNotSame(e1, _repo.Single(e => e.Id == e1.Id));
            Assert.IsTrue(CompareEntities(e1, _repo.Single(e => e.Id == e1.Id)));

            _repo.Insert(new TestEntity[] { e2, e3 });

            Assert.AreEqual(3, _repo.Count());
            Assert.AreNotSame(e2, _repo.Single(e => e.Id == e2.Id));
            Assert.IsTrue(CompareEntities(e2, _repo.Single(e => e.Id == e2.Id)));
            Assert.AreNotSame(e3, _repo.Single(e => e.Id == e3.Id));
            Assert.IsTrue(CompareEntities(e3, _repo.Single(e => e.Id == e3.Id)));
        }

        [Test]
        public void TestUpdating()
        {
            TestEntity e1 = CreateEntity(_i++);
            _repo.InsertOrUpdate(e1);

            Assert.AreEqual(1, _repo.Count());
            Assert.AreNotSame(e1, _repo.Single(e => e.Id == e1.Id));

            _repo.InsertOrUpdate(e1);

            Assert.AreEqual(1, _repo.Count());
            Assert.AreNotSame(e1, _repo.Single(e => e.Id == e1.Id));
        }

        [Test]
        public void TestInsertingOrUpdating()
        {
            TestEntity e1 = CreateEntity(_i++);
            _repo.Insert(e1);

            Assert.AreEqual(1, _repo.Count());
            Assert.AreNotSame(e1, _repo.Single(e => e.Id == e1.Id));

            ModifyEntity(e1);

            _repo.Update(e1);

            Assert.AreEqual(1, _repo.Count());
            Assert.AreNotSame(e1, _repo.Single(e => e.Id == e1.Id));
        }

        [Test]
        public void TestDeleting1()
        {
            TestEntity e1 = CreateEntity(_i++);
            _repo.Insert(e1);

            Assert.AreEqual(1, _repo.Count());
            Assert.AreNotSame(e1, _repo.Single(e => e.Id == e1.Id));

            _repo.Delete(Guid.Empty);

            Assert.AreEqual(1, _repo.Count());
            Assert.AreNotSame(e1, _repo.Single(e => e.Id == e1.Id));

            _repo.Delete(e1);

            Assert.AreEqual(0, _repo.Count());
        }

        [Test]
        public void TestDeleting2()
        {
            TestEntity e1 = CreateEntity(_i++);
            _repo.Insert(e1);

            Assert.AreEqual(1, _repo.Count());
            Assert.AreNotSame(e1, _repo.Single(e => e.Id == e1.Id));

            _repo.Delete(Guid.Empty);

            Assert.AreEqual(1, _repo.Count());
            Assert.AreNotSame(e1, _repo.Single(e => e.Id == e1.Id));

            _repo.Delete(e1.Id);

            Assert.AreEqual(0, _repo.Count());
        }

        private static IRepository<TestEntity> CreateRepository(string connectionString)
        {
            return new TestRepository(connectionString, "testentities-" + Guid.NewGuid().ToString());
        }

        private static TestEntity CreateEntity(int i)
        {
            return new TestEntity()
            {
                Id = Guid.NewGuid(),
                Name = string.Format("testentity-{0}", i)
            };
        }

        private static void ModifyEntity(TestEntity e)
        {
            e.Name = e.Name + "-modified";
        }

        private static bool CompareEntities(TestEntity a, TestEntity b)
        {
            return a.Id == b.Id && a.Name == b.Name;
        }
    }

    public class TestEntity : IEntity
    {
        public Guid Id { get; set; }

        public string Name { get; set; }
    }

    public class TestRepository : MongoDbRepository<TestEntity>, IRepository<TestEntity>
    {
        public TestRepository(string connectionString, string collectionName)
            : base(connectionString, collectionName)
        {
        }
    }
}
