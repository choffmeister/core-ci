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
using System.Text;

namespace CoreCI.Common.Shell
{
    public class MemoryShellOutput : IShellOutput
    {
        private readonly StringBuilder standardOutput = new StringBuilder();
        private readonly StringBuilder standardError = new StringBuilder();

        public string StandardOutput
        {
            get { return this.standardOutput.ToString(); }
        }

        public string StandardError
        {
            get { return this.standardError.ToString(); }
        }

        public void WriteStandardOutput(string s)
        {
            this.standardOutput.Append(s);
        }

        public void WriteStandardError(string s)
        {
            this.standardError.Append(s);
        }

        public void Dispose()
        {
        }
    }
}
