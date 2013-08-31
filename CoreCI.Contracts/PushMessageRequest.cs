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
using ServiceStack.ServiceHost;

namespace CoreCI.Contracts
{
    [Route("/push", Verbs = "POST")]
    public class PushMessageRequest : IReturn<PushMessageResponse>
    {
        public Guid? ClientId { get; set; }
    }

    public class PushMessageResponse
    {
        public Guid ClientId { get; set; }

        public List<PushMessage> Messages { get; set; }

        public PushMessageResponse()
        {
            this.Messages = new List<PushMessage>();
        }

        public PushMessageResponse(Guid clientId)
            : this()
        {
            this.ClientId = clientId;
        }
    }

    /// <summary>
    /// A simple data container for push messages.
    /// </summary>
    public class PushMessage
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>
        /// The id.
        /// </value>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the created on.
        /// </summary>
        /// <value>
        /// The created on.
        /// </value>
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PushMessage"/> class.
        /// </summary>
        public PushMessage()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PushMessage"/> class.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public PushMessage(long id, string key, object value)
            : this()
        {
            this.Id = id;
            this.Key = key;
            this.Value = value;
            this.CreatedOn = DateTime.UtcNow;
        }
    }
}
