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
using System.Threading;

namespace CoreCI.Common
{
    /// <summary>
    /// A wrapper for a single action that has to be repeated
    /// over and over again in a seperate thread. A dispatcher
    /// has to be given that returns the working items passt to
    /// the action. Concurrent execution is supported.
    /// </summary>
    /// <typeparam name="T">The type of the object given from the dispatcher to a worker.</typeparam>
    public class ConcurrentTaskLoop<T>
        where T : class
    {
        private readonly object lockObject = new object();
        private readonly int millisecondsIdleSleep;
        private readonly int parallelThreads;
        private readonly Func<T> dispatcher;
        private readonly Action<T> action;
        private Thread dispatcherThread;
        private Thread[] workerThreads;
        private T[] workItems;
        private bool isStopped;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentTaskLoop{T}" /> class.
        /// Creates a new loop task executing an action over and over again.
        /// The action is passed an work item, that is received from a dispatcher.
        /// The dispatcher method can return null, to indicate that there is
        /// no new working item right now.
        /// </summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="action">The action to execute.</param>
        /// <param name="millisecondsIdleSleep">The time to sleep in milliseconds between to invocations of the dispatcher.</param>
        /// <param name="parallelThreads">The number of concurrent tasks.</param>
        public ConcurrentTaskLoop(Func<T> dispatcher, Action<T> action, int millisecondsIdleSleep = 100, int parallelThreads = 1)
        {
            this.dispatcher = dispatcher;
            this.action = action;
            this.millisecondsIdleSleep = millisecondsIdleSleep;
            this.parallelThreads = parallelThreads;
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

                // create work item slots
                this.workItems = new T[this.parallelThreads];

                // start work threads
                this.workerThreads = Enumerable.Range(0, this.parallelThreads)
                    .Select(i => new Thread(this.Worker))
                        .ToArray();
                for (int i = 0; i < this.parallelThreads; i++)
                {
                    this.workerThreads[i].Start(i);
                }

                // start thread
                this.dispatcherThread = new Thread(this.Dispatcher);
                this.dispatcherThread.Start();
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
                this.dispatcherThread.Join();
                this.dispatcherThread = null;

                foreach (Thread workerThread in this.workerThreads)
                {
                    workerThread.Join();
                }

                this.workerThreads = null;
                this.workItems = null;
            }
        }

        /// <summary>
        /// Invokes the dispatcher to receive new work items.
        /// </summary>
        private void Dispatcher()
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

                        int? freeSlot = this.FindFreeSlot();

                        if (freeSlot.HasValue)
                        {
                            T workItem = this.dispatcher();

                            if (workItem != null)
                            {
                                this.workItems[freeSlot.Value] = workItem;
                                last = DateTime.MinValue;

                                // do not idle
                                continue;
                            }
                        }
                    }
                }
                catch
                {
                    // TODO: handle
                }

                Thread.Sleep(25);
            }
        }

        /// <summary>
        /// Concurrently processes a work item.
        /// </summary>
        /// <param name="state">The number of the thread.</param>
        private void Worker(object state)
        {
            int i = (int)state;

            // loop until marked as stopped
            while (!this.isStopped)
            {
                try
                {
                    if (this.workItems[i] != null)
                    {
                        T workItem = this.workItems[i];
                        this.workItems[i] = null;

                        this.action(workItem);
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

        private int? FindFreeSlot()
        {
            for (int i = 0; i < this.parallelThreads; i++)
            {
                if (this.workItems[i] == null)
                {
                    return i;
                }
            }

            return null;
        }
    }
}
