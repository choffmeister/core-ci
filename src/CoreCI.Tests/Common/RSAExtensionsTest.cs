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
using System.Linq;
using System.Security.Cryptography;
using CoreCI.Common;
using NUnit.Framework;
using Renci.SshNet.Security;
using Renci.SshNet;
using Mono.Unix;

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
        public void TestPrivateKeyCreation()
        {
            var rsa = new RSACryptoServiceProvider(1024);

            var privateKeyString = rsa.ToOpenSshPrivateKeyFileString();
            var privateKeyPath = Path.Combine(_tempFolder, "id_rsa");
            File.WriteAllText(privateKeyPath, privateKeyString);

            var result = ProcessHelper.Execute("openssl", "rsa -check -in " + privateKeyPath);

            Assert.AreEqual(0, result.ExitCode);
            Assert.That(result.StdOut, Is.StringStarting("RSA key ok"));

            string[] lines = privateKeyString.Split(new string[]
            {
                "\r\n",
                "\n"
            }, StringSplitOptions.RemoveEmptyEntries);
            string base64 = string.Join("", lines.Skip(1).Take(lines.Length - 2));
            byte[] binary = Convert.FromBase64String(base64);

            RsaKey rsaKey = new RsaKey(binary);

            Assert.AreEqual(1, rsaKey.D.Sign);
            Assert.AreEqual(1, rsaKey.DP.Sign);
            Assert.AreEqual(1, rsaKey.DQ.Sign);
            Assert.AreEqual(1, rsaKey.Exponent.Sign);
            Assert.AreEqual(1, rsaKey.InverseQ.Sign);
            Assert.AreEqual(1, rsaKey.Modulus.Sign);
            Assert.AreEqual(1, rsaKey.P.Sign);
            Assert.AreEqual(1, rsaKey.Q.Sign);
        }

        [Test]
        public void TestPublicKeyCreation()
        {
            var rsa = new RSACryptoServiceProvider(1024);

            var publicKeyString = rsa.ToOpenSshPublicKeyFileString("test@test");
            var privateKeyString = rsa.ToOpenSshPrivateKeyFileString();

            var publicKeyPath = Path.Combine(_tempFolder, "id_rsa.pub");
            var privateKeyPath = Path.Combine(_tempFolder, "id_rsa");

            File.WriteAllText(publicKeyPath, publicKeyString);
            File.WriteAllText(privateKeyPath, privateKeyString);

            var result = ProcessHelper.Execute("ssh-keygen", "-lf " + publicKeyPath);

            Assert.AreEqual(0, result.ExitCode);
        }

        [Test]
        public void TestPublicKeyPrivateKeyMatching()
        {
            var rsa = new RSACryptoServiceProvider(1024);

            var publicKeyString = rsa.ToOpenSshPublicKeyFileString("test@test");
            var privateKeyString = rsa.ToOpenSshPrivateKeyFileString();

            var publicKeyPath = Path.Combine(_tempFolder, "id_rsa.pub");
            var privateKeyPath = Path.Combine(_tempFolder, "id_rsa");

            File.WriteAllText(publicKeyPath, publicKeyString);
            File.WriteAllText(privateKeyPath, privateKeyString);

            // set chmod of private key to 0600, so that ssh-keygen does not complain
            var ufi = new UnixFileInfo(privateKeyPath);
            ufi.Protection = Mono.Unix.Native.FilePermissions.S_IRUSR | Mono.Unix.Native.FilePermissions.S_IWUSR;

            var result = ProcessHelper.Execute("ssh-keygen", "-yf " + privateKeyPath);

            Assert.AreEqual(0, result.ExitCode);
            Assert.AreEqual(result.Output + " test@test", publicKeyString);
        }
    }
}
