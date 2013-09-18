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
using NUnit.Framework;

namespace CoreCI.Tests.Common
{
    [TestFixture]
    public class BinaryExtensionsTest
    {
        [Test]
        public void TestGuidToUndashedString()
        {
            Guid guid1 = Guid.Empty;
            Guid guid2 = Guid.NewGuid();

            Assert.AreEqual("00000000000000000000000000000000", guid1.ToUndashedString());
            Assert.AreEqual(guid2.ToString().Replace("-", string.Empty).ToLowerInvariant(), guid2.ToUndashedString());
        }

        [Test]
        public void TestByteArrayToHexString()
        {
            byte[] bytes1 = new byte[0];
            byte[] bytes2 = new byte[] { 0 };
            byte[] bytes3 = new byte[] { 0, 1 };
            byte[] bytes4 = new byte[] { 0, 1, 254, 255 };

            Assert.AreEqual(string.Empty, bytes1.ToHexString());
            Assert.AreEqual("00", bytes2.ToHexString());
            Assert.AreEqual("0001", bytes3.ToHexString());
            Assert.AreEqual("0001feff", bytes4.ToHexString());
        }
    }
}
