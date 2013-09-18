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
using NUnit.Framework;

namespace CoreCI.Tests.Models
{
    [TestFixture]
    public class MongoDbRepositoryTest
    {
        private int i = 0;
        private IRepository<TestEntity> repo;

        [SetUpAttribute]
        public void SetUp()
        {
            this.repo = CreateRepository("Server=mongodb://localhost;Database=coreci-test");
        }

        [TearDownAttribute]
        public void TearDown()
        {
            this.repo.Clear();
            this.repo.Dispose();
        }

        [Test]
        public void TestInserting()
        {
            TestEntity e1 = CreateEntity(this.i++);
            TestEntity e2 = CreateEntity(this.i++);
            TestEntity e3 = CreateEntity(this.i++);

            Assert.AreEqual(0, this.repo.Count());

            this.repo.Insert(e1);

            Assert.AreEqual(1, this.repo.Count());
            Assert.AreNotSame(e1, this.repo.Single(e => e.Id == e1.Id));
            Assert.IsTrue(CompareEntities(e1, this.repo.Single(e => e.Id == e1.Id)));

            this.repo.Insert(new TestEntity[] { e2, e3 });

            Assert.AreEqual(3, this.repo.Count());
            Assert.AreNotSame(e2, this.repo.Single(e => e.Id == e2.Id));
            Assert.IsTrue(CompareEntities(e2, this.repo.Single(e => e.Id == e2.Id)));
            Assert.AreNotSame(e3, this.repo.Single(e => e.Id == e3.Id));
            Assert.IsTrue(CompareEntities(e3, this.repo.Single(e => e.Id == e3.Id)));
        }

        [Test]
        public void TestUpdating()
        {
            TestEntity e1 = CreateEntity(this.i++);
            this.repo.InsertOrUpdate(e1);

            Assert.AreEqual(1, this.repo.Count());
            Assert.AreNotSame(e1, this.repo.Single(e => e.Id == e1.Id));

            this.repo.InsertOrUpdate(e1);

            Assert.AreEqual(1, this.repo.Count());
            Assert.AreNotSame(e1, this.repo.Single(e => e.Id == e1.Id));
        }

        [Test]
        public void TestInsertingOrUpdating()
        {
            TestEntity e1 = CreateEntity(this.i++);
            this.repo.Insert(e1);

            Assert.AreEqual(1, this.repo.Count());
            Assert.AreNotSame(e1, this.repo.Single(e => e.Id == e1.Id));

            ModifyEntity(e1);

            this.repo.Update(e1);

            Assert.AreEqual(1, this.repo.Count());
            Assert.AreNotSame(e1, this.repo.Single(e => e.Id == e1.Id));
        }

        [Test]
        public void TestDeleting1()
        {
            TestEntity e1 = CreateEntity(this.i++);
            this.repo.Insert(e1);

            Assert.AreEqual(1, this.repo.Count());
            Assert.AreNotSame(e1, this.repo.Single(e => e.Id == e1.Id));

            this.repo.Delete(Guid.Empty);

            Assert.AreEqual(1, this.repo.Count());
            Assert.AreNotSame(e1, this.repo.Single(e => e.Id == e1.Id));

            this.repo.Delete(e1);

            Assert.AreEqual(0, this.repo.Count());
        }

        [Test]
        public void TestDeleting2()
        {
            TestEntity e1 = CreateEntity(this.i++);
            this.repo.Insert(e1);

            Assert.AreEqual(1, this.repo.Count());
            Assert.AreNotSame(e1, this.repo.Single(e => e.Id == e1.Id));

            this.repo.Delete(Guid.Empty);

            Assert.AreEqual(1, this.repo.Count());
            Assert.AreNotSame(e1, this.repo.Single(e => e.Id == e1.Id));

            this.repo.Delete(e1.Id);

            Assert.AreEqual(0, this.repo.Count());
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
