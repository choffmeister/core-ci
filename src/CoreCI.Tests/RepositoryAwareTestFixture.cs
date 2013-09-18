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
using CoreCI.Models;
using NUnit.Framework;

namespace CoreCI.Tests
{
    public class RepositoryAwareTestFixture
    {
        private IConnectorRepository connectorRepository;
        private IUserRepository userRepository;
        private IWorkerRepository workerRepository;
        private IProjectRepository projectRepository;
        private ITaskRepository taskRepository;
        private ITaskShellRepository taskShellRepository;

        public IConnectorRepository ConnectorRepository
        {
            get { return this.connectorRepository; }
        }

        public IUserRepository UserRepository
        {
            get { return this.userRepository; }
        }

        public IWorkerRepository WorkerRepository
        {
            get { return this.workerRepository; }
        }

        public IProjectRepository ProjectRepository
        {
            get { return this.projectRepository; }
        }

        public ITaskRepository TaskRepository
        {
            get { return this.taskRepository; }
        }

        public ITaskShellRepository TaskShellRepository
        {
            get { return this.taskShellRepository; }
        }

        [SetUpAttribute]
        public void SetUp()
        {
            string connectionString = "Server=mongodb://localhost;Database=coreci-test";

            this.connectorRepository = CoreCI.Models.ConnectorRepository.CreateTemporary(connectionString);
            this.userRepository = CoreCI.Models.UserRepository.CreateTemporary(connectionString);
            this.workerRepository = CoreCI.Models.WorkerRepository.CreateTemporary(connectionString);
            this.projectRepository = CoreCI.Models.ProjectRepository.CreateTemporary(connectionString);
            this.taskRepository = CoreCI.Models.TaskRepository.CreateTemporary(connectionString);
            this.taskShellRepository = CoreCI.Models.TaskShellRepository.CreateTemporary(connectionString);
        }

        [TearDown]
        public void TearDown()
        {
            this.connectorRepository.Clear();
            this.userRepository.Clear();
            this.workerRepository.Clear();
            this.projectRepository.Clear();
            this.taskRepository.Clear();
            this.taskShellRepository.Clear();

            this.connectorRepository.Dispose();
            this.userRepository.Dispose();
            this.workerRepository.Dispose();
            this.projectRepository.Dispose();
            this.taskRepository.Dispose();
            this.taskShellRepository.Dispose();
        }
    }
}
