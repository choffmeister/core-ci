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
using CoreCI.Worker.VirtualMachines;
using System.IO;
using System.Threading.Tasks;
using CoreCI.Common;

namespace CoreCI.Tests.Worker
{
    [TestFixture]
    public class VagrantVirtualMachineTest
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
        public void TestCreationOfVirtualMachine()
        {
            using (var vm = new VagrantVirtualMachine("vagrant", _tempFolder, "precise64", new Uri("http://boxes.choffmeister.de/precise64.box"), 2, 1024))
            {
                vm.Up();

                using (var shell = vm.CreateClient())
                {
                    shell.Connect();

                    var command = shell.CreateCommand("id");
                    var output = command.Execute();

                    Assert.AreEqual(0, command.ExitStatus);
                    Assert.That(output, Is.StringContaining("uid"));
                    Assert.That(output, Is.StringContaining("coreci"));

                    shell.Disconnect();
                }

                vm.Down();
            }
        }

        [Test]
        public void TestParallelCreationOfVirtualMachine()
        {
            var t1 = Task.Factory.StartNew(this.TestCreationOfVirtualMachine);
            var t2 = Task.Factory.StartNew(this.TestCreationOfVirtualMachine);

            Task.WaitAll(t1, t2);
        }

        [Test]
        [ExpectedException(typeof(VirtualMachineException))]
        public void TestExceptionForUnknownBox()
        {
            using (var vm = new VagrantVirtualMachine("vagrant", _tempFolder, "unknown", new Uri("http://boxes.choffmeister.de/unknown.box"), 2, 1024))
            {
                vm.Up();
                vm.Down();
            }
        }
    }
}
