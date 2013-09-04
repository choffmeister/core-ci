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
using System.Runtime.Serialization;

namespace CoreCI.Worker.Shell
{
    [Serializable]
    public class ShellCommandFailedException : Exception
    {
        private readonly int _exitCode;

        public int ExitCode
        {
            get { return _exitCode; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SshCommandFailedException"/> class
        /// </summary>
        public ShellCommandFailedException(int exitCode)
        {
            _exitCode = exitCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SshCommandFailedException"/> class
        /// </summary>
        /// <param name="message">A <see cref="T:System.String"/> that describes the exception. </param>
        public ShellCommandFailedException(int exitCode, string message)
            : base (message)
        {
            _exitCode = exitCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SshCommandFailedException"/> class
        /// </summary>
        /// <param name="message">A <see cref="T:System.String"/> that describes the exception. </param>
        /// <param name="inner">The exception that is the cause of the current exception. </param>
        public ShellCommandFailedException(int exitCode, string message, Exception inner)
            : base (message, inner)
        {
            _exitCode = exitCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SshCommandFailedException"/> class
        /// </summary>
        /// <param name="context">The contextual information about the source or destination.</param>
        /// <param name="info">The object that holds the serialized object data.</param>
        protected ShellCommandFailedException(SerializationInfo info, StreamingContext context)
            : base (info, context)
        {
        }
    }
}
