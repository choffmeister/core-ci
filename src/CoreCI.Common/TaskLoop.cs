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
        private readonly object lockObject = new object();
        private readonly int millisecondsIdleSleep;
        private readonly Func<bool> action;
        private Thread thread;
        private bool isStopped;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskLoop"/> class.
        /// Creates a new loop task executing an action over and over again.
        /// If the action returns false, the looping is idled for some time.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="millisecondsIdleSleep">The time to sleep in milliseconds if the action returns false.</param>
        public TaskLoop(Func<bool> action, int millisecondsIdleSleep)
        {
            this.action = action;
            this.millisecondsIdleSleep = millisecondsIdleSleep;
            this.isStopped = true;
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
            lock (this.lockObject)
            {
                // throw exception if already stopped
                if (this.isStopped == false)
                {
                    throw new InvalidOperationException();
                }

                // mark as started
                this.isStopped = false;

                // start thread
                this.thread = new Thread(this.Loop);
                this.thread.Start();
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
            lock (this.lockObject)
            {
                // throw exception if already started
                if (this.isStopped == true)
                {
                    throw new InvalidOperationException();
                }

                // mark as stopped
                this.isStopped = true;

                // block calling thread until loop thread has finished
                this.thread.Join();
                this.thread = null;
            }
        }

        /// <summary>
        /// Loops the action over and over again.
        /// </summary>
        private void Loop()
        {
            DateTime last = DateTime.MinValue;

            // loop until marked as stopped
            while (!this.isStopped)
            {
                try
                {
                    DateTime now = DateTime.Now;

                    if ((now - last).TotalMilliseconds > this.millisecondsIdleSleep)
                    {
                        last = now;

                        if (this.action())
                        {
                            last = DateTime.MinValue;
                        }
                    }
                    else
                    {
                        Thread.Sleep(25);
                    }
                }
                catch
                {
                    // TODO: handle
                }
            }
        }
    }
}
