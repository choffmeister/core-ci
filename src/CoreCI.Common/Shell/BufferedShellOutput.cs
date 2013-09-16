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
using System.IO;
using System.Collections.Generic;
using Renci.SshNet;
using System.Text;

namespace CoreCI.Common.Shell
{
    public class BufferedShellOutput : IShellOutput
    {
        private readonly object _lock = new object();
        private StringBuilder _previousContent;
        private StringBuilder _currentContent;
        private int _column;
        private int _row;

        public string Text
        {
            get
            {
                lock (_lock)
                {
                    return _previousContent.ToString() + _currentContent.ToString(); 
                }
            }
        }

        public BufferedShellOutput()
        {
            _previousContent = new StringBuilder();
            _currentContent = new StringBuilder();
            _column = 0;
            _row = 0;
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
            lock (_lock)
            {
                if (string.IsNullOrEmpty(s))
                {
                    return;
                }

                for (int i = 0; i < s.Length; i++)
                {
                    if (s [i] == '\r')
                    {
                        _column = 0;
                    }
                    else if (s [i] == '\n')
                    {
                        _previousContent.Append(_currentContent.ToString());
                        _previousContent.Append('\n');
                        _currentContent.Clear();
                        _column = 0;
                        _row++;
                    }
                    else
                    {
                        if (_column == _currentContent.Length)
                        {
                            _currentContent.Append(s [i]);
                        }
                        else
                        {
                            _currentContent [_column] = s [i];
                        }

                        _column++;
                    }
                }
            }
        }
    }
}
