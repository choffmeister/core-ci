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
using CoreCI.WorkerInstance.Vagrant;
using Renci.SshNet.Common;
using CoreCI.Common;
using CoreCI.Common.Shell;
using System.Text;

namespace CoreCI.Tests.WorkerInstance.Vagrant
{
    [TestFixture()]
    public class SshClientExtensionsTest
    {
        private string _tempFolder;

        [TestFixtureSetUp]
        public void SetUp()
        {
            try
            {
                // try if vagrant --version can be executed
                var result = ProcessHelper.Execute("vagrant", "--version");
                Assert.AreEqual(0, result.ExitCode);
                Assert.That(result.StdOut, Is.StringStarting("Vagrant version"));
            }
            catch
            {
                Assert.Ignore("Ignored because Vagrant binary is not in the PATH");
            }

            _tempFolder = TemporaryHelper.CreateTempFolder();
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            TemporaryHelper.DeleteTempFolder(_tempFolder);
        }

        [Test]
        public void TestCommandExecution()
        {
            using (var vm = new VagrantVirtualMachine("vagrant", _tempFolder, "precise64", new Uri("http://files.vagrantup.com/precise64.box"), 2, 1024))
            {
                vm.Up();

                using (var shell = vm.CreateClient())
                {
                    shell.Connect();

                    {
                        MemoryShellOutput shellOutput = new MemoryShellOutput();
                        int exitCode = shell.Execute("echo Hello World", shellOutput, TimeSpan.FromSeconds(15));

                        Assert.AreEqual(0, exitCode);
                        Assert.AreEqual("Hello World", shellOutput.StandardOutput);
                    }

                    {
                        MemoryShellOutput shellOutput = new MemoryShellOutput();
                        int exitCode = shell.Execute("thisisaunknowncommand", shellOutput, TimeSpan.FromSeconds(15));

                        Assert.AreNotEqual(0, exitCode);
                    }

                    shell.Disconnect();
                }

                vm.Down();
            }
        }

        [Test]
        public void TestCommandTimeout()
        {
            using (var vm = new VagrantVirtualMachine("vagrant", _tempFolder, "precise64", new Uri("http://files.vagrantup.com/precise64.box"), 2, 1024))
            {
                vm.Up();

                using (var shell = vm.CreateClient())
                {
                    shell.Connect();

                    DateTime start = DateTime.Now;

                    try
                    {
                        shell.Execute("sleep 10", new NullShellOutput(), TimeSpan.FromSeconds(1));

                        Assert.Fail();
                    }
                    catch (SshOperationTimeoutException)
                    {
                        // this exception is expected
                    }

                    DateTime end = DateTime.Now;

                    Assert.That((int)((end - start).TotalMilliseconds), Is.InRange(1000 - 200, 1000 + 200));

                    shell.Disconnect();
                }

                vm.Down();
            }
        }
    }
}
