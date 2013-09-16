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
using System.Configuration;

namespace CoreCI.Common
{
    public class FileConfigurationProvider : IConfigurationProvider
    {
        public virtual string GetConnectionString(string name, bool throwIfNotExistent = true)
        {
            ConnectionStringSettings connectionString = ConfigurationManager.ConnectionStrings [name];

            if (connectionString == null && throwIfNotExistent)
            {
                throw new Exception(string.Format("Mandatory connection string '{0}' not existent", name));
            }

            return connectionString != null ? connectionString.ConnectionString : null;
        }

        public virtual string GetSettingString(string key, bool throwIfNotExistent = true)
        {
            string settingString = ConfigurationManager.AppSettings [key];

            if (settingString == null && throwIfNotExistent)
            {
                throw new Exception(string.Format("Mandatory setting string '{0}' not existent", key));
            }

            return settingString;
        }

        public virtual string GetPath(string key, bool throwIfNotExistent = true)
        {
            return this.GetSettingString(key, throwIfNotExistent);
        }

        public void Dispose()
        {
        }
    }
}
