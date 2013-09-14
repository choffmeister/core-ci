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
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Text;

namespace CoreCI.Common.Shell
{
    public static class ShellExtensions
    {
        public static IEnumerable<string> SplitIntoCommandLines(string script)
        {
            script = script != null ? script + "\n" : "\n";

            StringBuilder sb = new StringBuilder();
            StringReader reader = new StringReader(script);
            string rawLine = null;

            while ((rawLine = reader.ReadLine()) != null)
            {
                if (rawLine.EndsWith("\\"))
                {
                    sb.Append(rawLine.Substring(0, rawLine.Length - 1));
                }
                else
                {
                    sb.Append(rawLine);
                    string line = sb.ToString();
                    sb.Clear();

                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        yield return line;
                    }
                }
            }
        }
    }
}
