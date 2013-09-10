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
using NUnit.Framework;
using CoreCI.Common;
using System.Security.Cryptography;
using System.IO;

namespace CoreCI.Tests.Common
{
    [TestFixture]
    public class RSAExtensionsTest
    {
        private string _tempFolder;

        [TestFixtureSetUp]
        public void SetUp()
        {
            try
            {
                // try if openssl version can be executed
                var result = ProcessHelper.Execute("openssl", "version");
                Assert.AreEqual(0, result.ExitCode);
                Assert.That(result.StdOut, Is.StringStarting("OpenSSL"));
            }
            catch
            {
                Assert.Ignore("Ignored because OpenSSL binary is not in the PATH");
            }

            _tempFolder = TemporaryHelper.CreateTempFolder();
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            TemporaryHelper.DeleteTempFolder(_tempFolder);
        }

        [Test]
        public void TestCase()
        {
            var rsa = new RSACryptoServiceProvider(1024);

            var publicKeyString = rsa.ToOpenSshPublicKeyFileString("test@test");
            var privateKeyString = rsa.ToOpenSshPrivateKeyFileString();

            var publicKeyPath = Path.Combine(_tempFolder, "id_rsa.pub");
            var privateKeyPath = Path.Combine(_tempFolder, "id_rsa");

            File.WriteAllText(publicKeyPath, publicKeyString);
            File.WriteAllText(privateKeyPath, privateKeyString);

            var result = ProcessHelper.Execute("openssl", "rsa -check -in " + privateKeyPath);

            Assert.AreEqual(0, result.ExitCode);
            Assert.That(result.StdOut, Is.StringStarting("RSA key ok"));
        }
    }
}
