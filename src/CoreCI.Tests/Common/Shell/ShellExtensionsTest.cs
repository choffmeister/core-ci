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
using System.Linq;
using CoreCI.Common.Shell;
using NUnit.Framework;

namespace CoreCI.Tests.Common.Shell
{
    [TestFixture]
    public class ShellExtensionsTest
    {
        [Test]
        public void TestSplittingIntoCommandLines()
        {
            Assert.AreEqual(new string[] { }, Split(string.Empty));
            Assert.AreEqual(new string[] { "a" }, Split("a"));
            Assert.AreEqual(new string[] { "ab" }, Split("ab"));
            Assert.AreEqual(new string[] { "abc" }, Split("abc"));
            Assert.AreEqual(new string[] { "abc", "def" }, Split("abc\ndef"));
            Assert.AreEqual(new string[] { "abc", "def" }, Split("abc\r\ndef"));
            Assert.AreEqual(new string[] { "abcdef" }, Split("abc\\\ndef"));
            Assert.AreEqual(new string[] { "abcdef" }, Split("abc\\\r\ndef"));
            Assert.AreEqual(new string[] { "abc", "def" }, Split("abc\n\ndef"));
            Assert.AreEqual(new string[] { "abc", "def" }, Split("abc\r\n\r\ndef"));
            Assert.AreEqual(new string[] { "\"abc\ndef\"" }, Split("\"abc\ndef\""));
            Assert.AreEqual(new string[] { "\"abc\r\ndef\"" }, Split("\"abc\r\ndef\""));
            Assert.AreEqual(new string[] { "\'abc\ndef\'" }, Split("\'abc\ndef\'"));
            Assert.AreEqual(new string[] { "\'abc\r\ndef\'" }, Split("\'abc\r\ndef\'"));
            Assert.AreEqual(new string[] { "\"a\'bc\"", "def" }, Split("\"a\'bc\"\ndef"));
            Assert.AreEqual(new string[] { "\'a\"bc\'", "def" }, Split("\'a\"bc\'\ndef"));
        }

        private static string[] Split(string script)
        {
            return ShellExtensions.SplitIntoCommandLines(script).ToArray();
        }
    }
}
