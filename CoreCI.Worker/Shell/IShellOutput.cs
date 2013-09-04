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
using ServiceStack.ServiceClient.Web;
using CoreCI.Models;
using CoreCI.Contracts;
using System.Collections.Generic;

namespace CoreCI.Worker.Shell
{
    public interface IShellOutput : IDisposable
    {
        void WriteStandardInput(string s);

        void WriteStandardOutput(string s);

        void WriteStandardError(string s);
    }
}
