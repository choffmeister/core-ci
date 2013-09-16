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
        private IConnectorRepository _connectorRepository;
        private IUserRepository _userRepository;
        private IWorkerRepository _workerRepository;
        private IProjectRepository _projectRepository;
        private ITaskRepository _taskRepository;
        private ITaskShellRepository _taskShellRepository;

        public IConnectorRepository ConnectorRepository
        {
            get { return _connectorRepository; }
        }

        public IUserRepository UserRepository
        {
            get { return _userRepository; }
        }

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

            _connectorRepository = CoreCI.Models.ConnectorRepository.CreateTemporary(connectionString);
            _userRepository = CoreCI.Models.UserRepository.CreateTemporary(connectionString);
            _workerRepository = CoreCI.Models.WorkerRepository.CreateTemporary(connectionString);
            _projectRepository = CoreCI.Models.ProjectRepository.CreateTemporary(connectionString);
            _taskRepository = CoreCI.Models.TaskRepository.CreateTemporary(connectionString);
            _taskShellRepository = CoreCI.Models.TaskShellRepository.CreateTemporary(connectionString);
        }

        [TearDown]
        public void TearDown()
        {
            _connectorRepository.Clear();
            _userRepository.Clear();
            _workerRepository.Clear();
            _projectRepository.Clear();
            _taskRepository.Clear();
            _taskShellRepository.Clear();

            _connectorRepository.Dispose();
            _userRepository.Dispose();
            _workerRepository.Dispose();
            _projectRepository.Dispose();
            _taskRepository.Dispose();
            _taskShellRepository.Dispose();
        }
    }
}
