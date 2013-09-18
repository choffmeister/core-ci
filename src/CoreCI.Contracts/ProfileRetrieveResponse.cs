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
using System.Collections.Generic;
using CoreCI.Models;

namespace CoreCI.Contracts
{
    public class ProfileRetrieveResponse
    {
        public ProfileRetrieveResponse()
        {
            this.Connectors = new List<ConnectorEntity>();
            this.Projects = new List<ProjectEntity>();
        }

        public UserEntity User { get; set; }

        public List<ConnectorEntity> Connectors { get; set; }

        public List<ProjectEntity> Projects { get; set; }
    }
}
