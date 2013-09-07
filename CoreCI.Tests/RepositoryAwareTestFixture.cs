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
using CoreCI.Models;
using NUnit.Framework;

namespace CoreCI.Tests
{
    public class RepositoryAwareTestFixture
    {
        private IWorkerRepository _workerRepository;
        private IProjectRepository _projectRepository;
        private ITaskRepository _taskRepository;
        private ITaskShellRepository _taskShellRepository;

        public IWorkerRepository WorkerRepository
        {
            get { return _workerRepository; }
        }

        public IProjectRepository ProjectRepository
        {
            get { return _projectRepository; }
        }

        public ITaskRepository TaskRepository
        {
            get { return _taskRepository; }
        }

        public ITaskShellRepository TaskShellRepository
        {
            get { return _taskShellRepository; }
        }

        [SetUpAttribute]
        public void SetUp()
        {
            string connectionString = "Server=mongodb://localhost;Database=coreci-test";

            _workerRepository = new WorkerRepository(connectionString, "workers-" + Guid.NewGuid().ToString());
            _projectRepository = new ProjectRepository(connectionString, "projects-" + Guid.NewGuid().ToString());
            _taskRepository = new TaskRepository(connectionString, "tasks-" + Guid.NewGuid().ToString());
            _taskShellRepository = new TaskShellRepository(connectionString, "taskshells-" + Guid.NewGuid().ToString());
        }

        [TearDown]
        public void TearDown()
        {
            _workerRepository.Clear();
            _projectRepository.Clear();
            _taskRepository.Clear();
            _taskShellRepository.Clear();

            _workerRepository.Dispose();
            _projectRepository.Dispose();
            _taskRepository.Dispose();
            _taskShellRepository.Dispose();
        }
    }
}
