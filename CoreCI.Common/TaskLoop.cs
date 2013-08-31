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

namespace CoreCI.Common
{
    /// <summary>
    /// A wrapper for a single action that has to be repeated
    /// over and over again in a seperate thread. The action must
    /// return a boolean indicating if it should be repeated again
    /// immediately or if the thread can be sent to sleep for a while
    /// before reexecuting the action. This class is threadsafe.
    /// </summary>
    public class TaskLoop
    {
        private readonly object _lock = new object();
        private readonly int _millisecondsIdleSleep;
        private readonly Func<bool> _action;
        private Thread _thread;
        private bool _isStopped;

        /// <summary>
        /// Creates a new loop task executing an action over and over again.
        /// If the action returns false, the looping is idled for some time.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="millisecondsIdleSleep">The time to sleep in milliseconds if the action returns false.</param>
        public TaskLoop(Func<bool> action, int millisecondsIdleSleep)
        {
            _action = action;
            _millisecondsIdleSleep = millisecondsIdleSleep;
            _isStopped = true;
        }

        /// <summary>
        /// Loops the action over and over again.
        /// </summary>
        private void Loop()
        {
            // loop until marked as stopped
            while (!_isStopped)
            {
                try
                {
                    if (!_action())
                    {
                        // action indicates the sleep for a while before reexecuting
                        if (_millisecondsIdleSleep > 0)
                        {
                            // send thread to sleep
                            Thread.Sleep(_millisecondsIdleSleep);
                        }
                    }
                }
                catch
                {
                    // TODO: handle
                }
            }
        }

        /// <summary>
        /// Starts the looping. This method blocks until the looping
        /// has been started.
        /// </summary>
        /// <exception cref="InvalidOperationException">Is thrown, if this method is called
        /// while the looping is already running.</exception>
        public void Start()
        {
            // lock to make sure, that concurrent calls to this method from
            // different threads do not break the state
            lock (_lock)
            {
                // throw exception if already stopped
                if (_isStopped == false)
                {
                    throw new InvalidOperationException();
                }

                // mark as started
                _isStopped = false;

                // start thread
                _thread = new Thread(this.Loop);
                _thread.Start();
            }
        }

        /// <summary>
        /// Stopps the looping. This method blocks until the looping
        /// has been stopped.
        /// </summary>
        /// <exception cref="InvalidOperationException">Is thrown, if this method is called
        /// while the looping is already stopped.</exception>
        public void Stop()
        {
            // lock to make sure, that concurrent calls to this method from
            // different threads do not break the state
            lock (_lock)
            {
                // throw exception if already started
                if (_isStopped == true)
                {
                    throw new InvalidOperationException();
                }

                // mark as stopped
                _isStopped = true;

                // block calling thread until loop thread has finished
                _thread.Join();
                _thread = null;
            }
        }
    }
}
