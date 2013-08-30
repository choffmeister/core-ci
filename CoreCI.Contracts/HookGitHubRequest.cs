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
using ServiceStack.ServiceHost;

namespace CoreCI.Contracts
{
    [Route("/hook/github", "POST")]
    public class HookGitHubRequest : IReturn<HookGitHubResponse>
    {
        public HookGitHubRequestPayload Payload { get; set; }
    }

    public class HookGitHubResponse
    {
    }

    public class HookGitHubRequestPayload
    {
        public string After { get; set; }

        public string Before { get; set; }

        public PayloadRepository Repository { get; set; }

        public HookGitHubRequestPayload()
        {
        }

        public class PayloadRepository
        {
            public string Name { get; set; }

            public PayloadRepositoryOwner Owner { get; set; }

            public class PayloadRepositoryOwner
            {
                public string Name { get; set; }
            }
        }
    }
}
