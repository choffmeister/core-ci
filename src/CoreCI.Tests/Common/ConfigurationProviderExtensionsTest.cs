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
using CoreCI.Common;
using NUnit.Framework;

namespace CoreCI.Tests.Common
{
    [TestFixture]
    public class ConfigurationProviderExtensionsTest
    {
        private readonly string config1 = @"
test1: Hello World
test2: 123
test3:
  test4: 12.5
  test5: false
  test6: true
test7:
  - 1.5
  - 2.5
  - 3.5
";
        private string tempFolder;

        [TestFixtureSetUp]
        public void SetUp()
        {
            this.tempFolder = TemporaryHelper.CreateTempFolder();
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            if (this.tempFolder != null)
            {
                TemporaryHelper.DeleteTempFolder(this.tempFolder);
                this.tempFolder = null;
            }
        }

        [Test]
        public void TestConversion()
        {
            string yamlPath = Path.Combine(this.tempFolder, string.Format("{0}.yml", Guid.NewGuid()));

            File.WriteAllText(yamlPath, this.config1);

            YamlConfigurationProvider config = new YamlConfigurationProvider(yamlPath);

            Assert.AreEqual("Hello World", config.Get("test1"));
            Assert.AreEqual(123, config.Get<int>("test2"));
            Assert.AreEqual(12.5, config.Get<double>("test3.test4"));
            Assert.AreEqual(false, config.Get<bool>("test3.test5"));
            Assert.AreEqual(true, config.Get<bool>("test3.test6"));
            Assert.AreEqual(new double[] { 1.5, 2.5, 3.5 }, config.GetArray<double>("test7"));
        }
    }
}
