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
using System.Diagnostics;
using System.IO;

namespace CoreCI.Common
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

            StringWriter stdOut = new StringWriter();
            StringWriter stdError = new StringWriter();
            StringWriter output = new StringWriter();

            using (Process p = new Process())
            {
                p.StartInfo = psi;
                p.Start();

                p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    stdOut.Write(e.Data);
                    output.Write(e.Data);
                };
                p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    stdError.Write(e.Data);
                    output.Write(e.Data);
                };
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();

                return new ProcessResult()
                {
                    ExitCode = p.ExitCode,
                    StdOut = stdOut.ToString(),
                    StdError = stdError.ToString(),
                    Output = output.ToString()
                };
            }
        }
    }

    public class ProcessResult
    {
        public int ExitCode { get; set; }

        public bool Success
        {
            get { return this.ExitCode == 0; }
        }

        public string StdOut { get; set; }

        public string StdError { get; set; }

        public string Output { get; set; }
    }
}
