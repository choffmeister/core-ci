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
using System.Globalization;

namespace CoreCI.Common
{
    public static class ConfigurationProviderExtensions
    {
        public static T Get<T>(this IConfigurationProvider config, string name, bool throwIfNotExistent = true)
        {
            string stringValue = config.Get(name, throwIfNotExistent);

            if (stringValue != null)
            {
                return ConvertString<T>(stringValue);
            }

            return default(T);
        }

        public static T[] GetArray<T>(this IConfigurationProvider config, string name, bool throwIfNotExistent = true)
        {
            string[] stringValues = config.GetArray(name, throwIfNotExistent);

            if (stringValues != null)
            {
                return stringValues.Select(ConvertString<T>).ToArray();
            }

            return null;
        }

        private static T ConvertString<T>(string value)
        {
            if (value != null)
            {
                Type type = typeof(T);
                IFormatProvider format = CultureInfo.InvariantCulture;

                return (T)Convert.ChangeType(value, type, format);
            }

            return default(T);
        }
    }
}
