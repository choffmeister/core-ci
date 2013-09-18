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
using CoreCI.Contracts;
using CoreCI.Server.Managers;
using ServiceStack.ServiceInterface;

namespace CoreCI.Server.Services
{
    /// <summary>
    /// The push messages service.
    /// </summary>
    public class PushService : Service
    {
        public static void Push(string key, object value)
        {
            PushMessageManager.CreatePushMessage(key, value);
        }

        public PushMessageResponse Post(PushMessageRequest request)
        {
            Guid clientId = PushMessageManager.GetClient(request.ClientId);
            List<PushMessage> pushMessages = PushMessageManager.RetrievePushMessagesBlocking(clientId);

            return new PushMessageResponse()
            {
                ClientId = clientId,
                Messages = pushMessages
            };
        }
    }
}
