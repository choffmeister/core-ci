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
            char? quoted = null;

            int i = 0;

            while (i < script.Length)
            {
                char current = script [i];
                char? next = i + 1 < script.Length ? script [i + 1] : default(char?);
                char? nextnext = i + 2 < script.Length ? script [i + 2] : default(char?);

                if (current == '\r' && next == '\n' && quoted == null)
                {
                    string line = sb.ToString();
                    sb.Clear();

                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        yield return line;
                    }

                    i += 2;
                }
                else if (current == '\n' && quoted == null)
                {
                    string line = sb.ToString();
                    sb.Clear();

                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        yield return line;
                    }

                    i += 1;
                }
                else if (current == '\\' && next == '\r' && nextnext == '\n')
                {
                    i += 3;
                }
                else if (current == '\\' && next == '\n')
                {
                    i += 2;
                }
                else if (current == '"')
                {
                    if (quoted == null)
                    {
                        quoted = current;
                    }
                    else if (quoted == current)
                    {
                        quoted = null;
                    }

                    sb.Append(current);
                    i++;
                }
                else if (current == '\'')
                {
                    if (quoted == null)
                    {
                        quoted = current;
                    }
                    else if (quoted == current)
                    {
                        quoted = null;
                    }

                    sb.Append(current);
                    i++;
                }
                else
                {
                    sb.Append(current);
                    i++;
                }
            }
        }
    }
}
