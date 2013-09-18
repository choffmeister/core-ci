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
using System.Threading;
using CoreCI.Common;
using NUnit.Framework;

namespace CoreCI.Tests.Common
{
    [TestFixture]
    public class ConcurrentTaskLoopTest
    {
        [Test]
        public void TestLoopingSingleWorkerWithoutTasks()
        {
            var count = 0;
            var loop = new ConcurrentTaskLoop<Foo>(() => null, (foo) => count++, 100, 1);

            loop.Start();
            Thread.Sleep(250);
            loop.Stop();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void TestLoopingSingleWorkerWithTasks()
        {
            var count = 0;
            var loop = new ConcurrentTaskLoop<Foo>(() => new Foo(), (i) => count++, 100, 1);

            loop.Start();
            Thread.Sleep(1000);
            loop.Stop();

            Assert.That(count, Is.InRange(10 - 3, 10 + 3));
        }

        [Test]
        public void TestLoopingMultipleWorkersWithTasks1()
        {
            var count = 0;
            var loop = new ConcurrentTaskLoop<Foo>(() => new Foo(), (foo) => count++, 100, 4);

            loop.Start();
            Thread.Sleep(1000);
            loop.Stop();

            Assert.That(count, Is.InRange(40 - 12, 40 + 12));
        }

        [Test]
        public void TestLoopingMultipleWorkersWithTasks2()
        {
            var count = 0;
            var loop = new ConcurrentTaskLoop<Foo>(() => new Foo(), (foo) => count++, 100, 8);

            loop.Start();
            Thread.Sleep(1000);
            loop.Stop();

            Assert.That(count, Is.InRange(80 - 24, 80 + 24));
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestExceptionIfDoubleStarted()
        {
            var count = 0;
            var loop = new ConcurrentTaskLoop<Foo>(() => new Foo(), (foo) => count++);

            try
            {
                loop.Start();
                loop.Start();

                Assert.Fail();
            }
            finally
            {
                loop.Stop();
            }
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestExceptionIfDoubleStopped()
        {
            var count = 0;
            var loop = new ConcurrentTaskLoop<Foo>(() => new Foo(), (foo) => count++);

            loop.Start();
            loop.Stop();
            loop.Stop();

            Assert.Fail();
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestExceptionIfStoppedWhileUnstarted()
        {
            var count = 0;
            var loop = new ConcurrentTaskLoop<Foo>(() => new Foo(), (foo) => count++);

            loop.Stop();

            Assert.Fail();
        }

        [Test]
        public void TestFastStopping()
        {
            var count = 0;
            var loop = new ConcurrentTaskLoop<Foo>(() => new Foo(), (foo) => count++, 5000);

            DateTime start = DateTime.Now;
            loop.Start();
            Thread.Sleep(100);
            loop.Stop();
            DateTime end = DateTime.Now;

            Assert.That((end - start).TotalMilliseconds, Is.LessThan(100 + 200));
            Assert.AreEqual(1, count);
        }

        [Test]
        public void TestWaitingForWorkerOnStopping()
        {
            var count = 0;
            var loop = new ConcurrentTaskLoop<Foo>(() => new Foo(), (foo) =>
            {
                Thread.Sleep(1000);
                count++;
            }, 5000, 1);

            DateTime start = DateTime.Now;
            loop.Start();
            Thread.Sleep(100);
            loop.Stop();
            DateTime end = DateTime.Now;

            Assert.That((end - start).TotalMilliseconds, Is.GreaterThan(1000 - 200));
            Assert.That((end - start).TotalMilliseconds, Is.LessThan(1000 + 200));
            Assert.AreEqual(1, count);
        }

        private class Foo
        {
        }
    }
}
