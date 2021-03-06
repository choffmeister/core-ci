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
    public class YamlConfigurationProviderTest
    {
        private readonly string config1 = File.ReadAllText("Resources/yaml-config1.yml");
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
        public void TestGet()
        {
            string yamlPath = Path.Combine(this.tempFolder, string.Format("{0}.yml", Guid.NewGuid()));

            File.WriteAllText(yamlPath, this.config1);

            YamlConfigurationProvider config = new YamlConfigurationProvider(yamlPath);

            Assert.AreEqual("Hello World", config.Get("test1-1"));
            Assert.AreEqual("Hello World\nFoobar\n", config.Get("test1-2"));
        }

        [Test]
        public void TestGetArray()
        {
            string yamlPath = Path.Combine(this.tempFolder, string.Format("{0}.yml", Guid.NewGuid()));

            File.WriteAllText(yamlPath, this.config1);

            YamlConfigurationProvider config = new YamlConfigurationProvider(yamlPath);

            Assert.AreEqual(new string[] { "apple", "pie" }, config.GetArray("test2-1"));
        }

        [Test]
        public void TestGetHierarchic()
        {
            string yamlPath = Path.Combine(this.tempFolder, string.Format("{0}.yml", Guid.NewGuid()));

            File.WriteAllText(yamlPath, this.config1);

            YamlConfigurationProvider config = new YamlConfigurationProvider(yamlPath);

            Assert.AreEqual("Hello World2", config.Get("test3.test1"));
            Assert.AreEqual("Hello World2\nFoobar2\n", config.Get("test3.test2"));
            Assert.AreEqual("Foobar2", config.Get("test3.test3.test4"));
        }

        [Test]
        public void TestGetArrayHierarchic()
        {
            string yamlPath = Path.Combine(this.tempFolder, string.Format("{0}.yml", Guid.NewGuid()));

            File.WriteAllText(yamlPath, this.config1);

            YamlConfigurationProvider config = new YamlConfigurationProvider(yamlPath);

            Assert.AreEqual(new string[] { "apple2" }, config.GetArray("test4.test1"));
            Assert.AreEqual(new string[] { "apple2", "pie2" }, config.GetArray("test4.test2"));
            Assert.AreEqual(new string[] { "pie2" }, config.GetArray("test4.test3.test4"));
        }
    }
}
