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
using System.Threading.Tasks;

namespace CoreCI.Tests.Models
{
    [TestFixture]
    public class TaskRepositoryTest : RepositoryAwareTestFixture
    {
        [Test]
        public void TestThreadSafetyOfPendingTaskFetching()
        {
            var concurrency = 1000;

            var tasks = Enumerable.Range(0, concurrency).Select(i => new TaskEntity()
            {
                Id = Guid.NewGuid(),
                State = TaskState.Pending
            }).ToList();

            this.TaskRepository.Insert(tasks);

            var workers = new Task[concurrency];
            var workerTasks = new TaskEntity[concurrency];

            for (int i = 0; i < concurrency; i++)
            {
                int j = i;
                Guid workerId = Guid.NewGuid();

                workers [j] = new Task(() =>
                {
                    workerTasks [j] = this.TaskRepository.GetPendingTask(workerId);
                });
            }

            for (int i = 0; i < concurrency; i++)
            {
                workers [i].Start();
            }

            Task.WaitAll(workers);

            Assert.AreEqual(0, this.TaskRepository.Count(t => t.State == TaskState.Pending), "All tasks should have been started");
        }
    }
}
