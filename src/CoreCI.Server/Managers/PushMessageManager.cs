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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreCI.Contracts;

namespace CoreCI.Server.Managers
{
    /// <summary>
    /// A static class to allow client notification via long polling (see
    /// <see cref="!:http://en.wikipedia.org/wiki/Pushthis.technology#Long_polling"/>).
    /// This class is threadsafe.
    /// </summary>
    public static class PushMessageManager
    {
        private static readonly object ClientIdLock = new object();
        private static readonly Dictionary<Guid, long> Clients = new Dictionary<Guid, long>();
        private static readonly List<PushMessage> PushMessages = new List<PushMessage>();
        private static long nextPushMessageId = 1;

        /// <summary>
        /// Gets a client id.
        /// </summary>
        /// <param name="clientId">The current client id or null.</param>
        /// <returns>A new client id.</returns>
        public static Guid GetClient(Guid? clientId)
        {
            lock (ClientIdLock)
            {
                if (clientId.HasValue && Clients.ContainsKey(clientId.Value))
                {
                    // if a cliend id has been passed in and if this id is already
                    // known, then just return the given id
                    return clientId.Value;
                }
                else
                {
                    // extract the number of the last push message
                    long currentPushMessageId = nextPushMessageId - 1;

                    // create a new client id
                    Guid newClientId = Guid.NewGuid();

                    // save the new id together with the last push message number
                    Clients.Add(newClientId, currentPushMessageId);

                    return newClientId;
                }
            }
        }

        /// <summary>
        /// Creates the push message.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void CreatePushMessage(string key, object value)
        {
            // delegate creation of new message to a seperate thread
            // since because of locking this could take some time
            Task.Factory.StartNew(() =>
            {
                lock (ClientIdLock)
                {
                    // get the next unused message number
                    long newPushMessageId = nextPushMessageId++;

                    // create a new message with the new number as identifier
                    PushMessages.Add(new PushMessage(newPushMessageId, key, value));
                }
            });
        }

        /// <summary>
        /// Retrieves the new push messages.
        /// </summary>
        /// <param name="clientId">The client id.</param>
        /// <returns>The new push messages.</returns>
        public static List<PushMessage> RetrievePushMessages(Guid clientId)
        {
            lock (ClientIdLock)
            {
                // extract the number of the last push message
                long currentPushMessageId = nextPushMessageId - 1;

                // update client id with new number to ensure, that
                // no message is ever delivered twice to the same client
                long lastPushMessageId = Clients[clientId];
                Clients[clientId] = currentPushMessageId;

                // return all available message that have not been delivered to the client yet
                return PushMessages.Where(n => lastPushMessageId < n.Id && n.Id <= currentPushMessageId).ToList();
            }
        }

        /// <summary>
        /// Retrieves the new push messages. This methods blocks until there are new messages or
        /// the timeout has been exceeded..
        /// </summary>
        /// <param name="clientId">The client id.</param>
        /// <param name="timeoutSeconds">The timeout in seconds.</param>
        /// <returns>The new push messages.</returns>
        public static List<PushMessage> RetrievePushMessagesBlocking(Guid clientId, int timeoutSeconds = 30)
        {
            DateTime start = DateTime.Now;

            // block until there is a new message or the timeout has been exceeded
            while (Clients[clientId] >= nextPushMessageId - 1 && DateTime.Now.Subtract(start).TotalSeconds < timeoutSeconds)
            {
                Thread.Sleep(10);
            }

            Thread.Sleep(250);

            // return all new messages (may be empty)
            return RetrievePushMessages(clientId);
        }
    }
}
