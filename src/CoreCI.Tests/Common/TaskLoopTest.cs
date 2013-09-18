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
    public class TaskLoopTest
    {
        [Test]
        public void TestLoopingWithIdle()
        {
            var count = 0;
            var loop = new TaskLoop(() => SleepIncrementAndReturn(0, ref count, false), 100);

            loop.Start();
            Thread.Sleep(250);
            loop.Stop();

            Assert.That(count, Is.InRange(3 - 1, 3 + 1));
        }

        [Test]
        public void TestLoopingWithoutIdle()
        {
            var count = 0;
            var loop = new TaskLoop(() => SleepIncrementAndReturn(0, ref count, true), 100);

            loop.Start();
            Thread.Sleep(250);
            loop.Stop();

            Assert.That(count, Is.GreaterThan(1000));
        }

        [Test]
        public void TestFastStopping()
        {
            var count = 0;
            var loop = new TaskLoop(() => SleepIncrementAndReturn(0, ref count, false), 5000);

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
            var loop = new TaskLoop(() => SleepIncrementAndReturn(1000, ref count, true), 5000);

            DateTime start = DateTime.Now;
            loop.Start();
            Thread.Sleep(100);
            loop.Stop();
            DateTime end = DateTime.Now;

            Assert.That((end - start).TotalMilliseconds, Is.GreaterThan(1000 - 200));
            Assert.That((end - start).TotalMilliseconds, Is.LessThan(1000 + 200));
            Assert.AreEqual(1, count);
        }

        private static bool SleepIncrementAndReturn(int sleep, ref int i, bool result)
        {
            if (sleep > 0)
            {
                Thread.Sleep(sleep);
            }

            i++;

            return result;
        }
    }
}
