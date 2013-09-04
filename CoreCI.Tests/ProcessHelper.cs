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
using System.Diagnostics;

namespace CoreCI.Tests
{
    public static class ProcessHelper
    {
        public static ProcessResult Execute(string fileName, string arguments, string workingDirectory = "")
        {
            ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments);
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            psi.WorkingDirectory = workingDirectory;

            using (Process p = new Process())
            {
                p.StartInfo = psi;
                p.Start();
                p.WaitForExit();

                return new ProcessResult()
                {
                    ExitCode = p.ExitCode,
                    StdOut = p.StandardOutput.ReadToEnd(),
                    StdError = p.StandardError.ReadToEnd()
                };
            }
        }
    }

    public class ProcessResult
    {
        public int ExitCode { get; set; }

        public string StdOut { get; set; }

        public string StdError { get; set; }
    }
}
