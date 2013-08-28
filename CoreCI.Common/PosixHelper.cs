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
using Mono.Unix;
using Mono.Unix.Native;

namespace CoreCI.Common
{
    public static class UnixHelper
    {
        /// <summary>
        /// Waits for signal (SIG_INT or SIG_TERM).
        /// </summary>
        public static void WaitForSignal()
        {
            UnixSignal[] signals = new UnixSignal[]
            {
                new UnixSignal(Signum.SIGINT),
                new UnixSignal(Signum.SIGTERM),
            };

            // Wait for a unix signal
            for (bool exit = false; !exit;)
            {
                int id = UnixSignal.WaitAny(signals);

                if (id >= 0 && id < signals.Length)
                {
                    if (signals [id].IsSet)
                        exit = true;
                }
            }
        }
    }
}
