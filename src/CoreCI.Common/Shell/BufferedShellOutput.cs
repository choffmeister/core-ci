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
    public class BufferedShellOutput : IShellOutput
    {
        private readonly object lockObject = new object();
        private StringBuilder previousContent;
        private StringBuilder currentContent;
        private int column;
        private int row;

        public BufferedShellOutput()
        {
            this.previousContent = new StringBuilder();
            this.currentContent = new StringBuilder();
            this.column = 0;
            this.row = 0;
        }

        public string Text
        {
            get
            {
                lock (this.lockObject)
                {
                    return this.previousContent.ToString() + this.currentContent.ToString();
                }
            }
        }

        public virtual void Dispose()
        {
        }

        public virtual void WriteStandardOutput(string s)
        {
            this.Write(s);
        }

        public virtual void WriteStandardError(string s)
        {
            this.Write(s);
        }

        private void Write(string s)
        {
            lock (this.lockObject)
            {
                if (string.IsNullOrEmpty(s))
                {
                    return;
                }

                for (int i = 0; i < s.Length; i++)
                {
                    if (s[i] == '\r')
                    {
                        this.column = 0;
                    }
                    else if (s[i] == '\n')
                    {
                        this.previousContent.Append(this.currentContent.ToString());
                        this.previousContent.Append('\n');
                        this.currentContent.Clear();
                        this.column = 0;
                        this.row++;
                    }
                    else
                    {
                        if (this.column == this.currentContent.Length)
                        {
                            this.currentContent.Append(s[i]);
                        }
                        else
                        {
                            this.currentContent[this.column] = s[i];
                        }

                        this.column++;
                    }
                }
            }
        }
    }
}
