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
using Renci.SshNet;

namespace CoreCI.WorkerInstance.Vagrant
{
    /// <summary>
    /// The base interface for virtual machines.
    /// </summary>
    public interface IVirtualMachine : IDisposable
    {
        ConnectionInfo ConnectionInfo { get; }

        /// <summary>
        /// Creates and starts the virtual machine. Must not be called,
        /// when already up.
        /// </summary>
        void Up();

        /// <summary>
        /// Stops and destroys the virtual machine. Must not be called,
        /// when already down or not started yet.
        /// </summary>
        void Down();

        /// <summary>
        /// Creates a SSH client to connect to the virtual machine.
        /// </summary>
        /// <returns>The client.</returns>
        SshClient CreateClient();
    }
}
