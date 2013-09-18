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
using System.Collections.Generic;
using CoreCI.Common.Shell;
using NUnit.Framework;

namespace CoreCI.Tests.Common.Shell
{
    [TestFixture]
    public class BufferedShellOutputTest
    {
        [Test]
        public void TestCase()
        {
            var expectedValues = new Dictionary<string[], string>()
            {
                { new string[] { string.Empty }, string.Empty },
                { new string[] { "Hello" }, "Hello" },
                { new string[] { "Hello\n" }, "Hello\n" },
                { new string[] { "Hello\n", "World" }, "Hello\nWorld" },
                { new string[] { "Hello\n", "World\n" }, "Hello\nWorld\n" },
                { new string[] { "Hello\n\n", "World\n" }, "Hello\n\nWorld\n" },
                { new string[] { "Hello\r\n" }, "Hello\n" },
                { new string[] { "Hello\r\n", "World" }, "Hello\nWorld" },
                { new string[] { "Hello\r\n", "World\r\n" }, "Hello\nWorld\n" },
                { new string[] { "Hello\r\n\r\n", "World\r\n" }, "Hello\n\nWorld\n" },
                { new string[] { "Hello", "World" }, "HelloWorld" },
                { new string[] { "Hello", "World\n" }, "HelloWorld\n" },
                { new string[] { "Hello", "World\r\n" }, "HelloWorld\n" },
                { new string[] { "Hello\rWorld" }, "World" },
                { new string[] { "Hello\r" }, "Hello" },
                { new string[] { "Hello\r1" }, "1ello" },
                { new string[] { "Hello\r12" }, "12llo" },
                { new string[] { "Hello\r123" }, "123lo" },
                { new string[] { "Hello\r1234" }, "1234o" },
                { new string[] { "Hello\r12345" }, "12345" },
                { new string[] { "Hello\r123456" }, "123456" },
            };

            foreach (var expectedValue in expectedValues)
            {
                BufferedShellOutput bso = new BufferedShellOutput();

                foreach (var part in expectedValue.Key)
                {
                    bso.WriteStandardOutput(part);
                }

                Assert.AreEqual(expectedValue.Value, bso.Text);
            }
        }
    }
}
