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
using CoreCI.Common;
using NLog;

namespace CoreCI.Worker
{
    /// <summary>
    /// Worker executable.
    /// </summary>
    public class WorkerExecutable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Main entry point.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        public static void Main(string[] args)
        {
            try
            {
                Log.Info("Starting");

                IConfigurationProvider configurationProvider = YamlConfigurationProvider.Default;
                WorkerHandler workerHandler = new WorkerHandler(configurationProvider);

                workerHandler.Start();
                UnixHelper.WaitForSignal();
                workerHandler.Stop();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex);
            }
            finally
            {
                Log.Info("Stopped");
            }

            // Mono bugfix, see http://nlog-project.org/2011/10/30/using-nlog-with-mono.html
            LogManager.Configuration = null;
        }
    }
}
