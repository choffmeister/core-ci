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
using System.Linq;
using CoreCI.Models;
using NUnit.Framework;
using ServiceStack.ServiceInterface.Testing;
using System.Threading.Tasks;
using CoreCI.Server.Connectors;
using CoreCI.Common;

namespace CoreCI.Tests.Server.Connectors
{
    [TestFixture]
    public class GitHubConnectorTest : RepositoryAwareTestFixture
    {
        private static readonly string _payload = @"{""ref"":""refs/heads/master"",""after"":""d231ce397c6fd3b2c853ed1ffce0f3fc1bc3d192"",""before"":""171880fd69f5e0fa4ccf010ec4deb0fa1911ee0a"",""created"":false,""deleted"":false,""forced"":false,""compare"":""https://github.com/choffmeister/hook-test/compare/171880fd69f5...d231ce397c6f"",""commits"":[{""id"":""612a38f878b322b89f8ec2dfafe3f0df51bbca69"",""distinct"":true,""message"":""Commit"",""timestamp"":""2013-08-30T10:53:51-07:00"",""url"":""https://github.com/choffmeister/hook-test/commit/612a38f878b322b89f8ec2dfafe3f0df51bbca69"",""author"":{""name"":""Christian Hoffmeister"",""email"":""mail@choffmeister.de"",""username"":""choffmeister""},""committer"":{""name"":""Christian Hoffmeister"",""email"":""mail@choffmeister.de"",""username"":""choffmeister""},""added"":[],""removed"":[],""modified"":[""README.md""]},{""id"":""2213fed29f5d6aac6230793d1f73b635607b29f5"",""distinct"":true,""message"":""Create .core-ci.yml"",""timestamp"":""2013-09-03T08:09:24-07:00"",""url"":""https://github.com/choffmeister/hook-test/commit/2213fed29f5d6aac6230793d1f73b635607b29f5"",""author"":{""name"":""Christian Hoffmeister"",""email"":""thekwasti@googlemail.com"",""username"":""choffmeister""},""committer"":{""name"":""Christian Hoffmeister"",""email"":""thekwasti@googlemail.com"",""username"":""choffmeister""},""added"":["".core-ci.yml""],""removed"":[],""modified"":[]},{""id"":""d231ce397c6fd3b2c853ed1ffce0f3fc1bc3d192"",""distinct"":true,""message"":""Update .core-ci.yml"",""timestamp"":""2013-09-03T08:20:27-07:00"",""url"":""https://github.com/choffmeister/hook-test/commit/d231ce397c6fd3b2c853ed1ffce0f3fc1bc3d192"",""author"":{""name"":""Christian Hoffmeister"",""email"":""thekwasti@googlemail.com"",""username"":""choffmeister""},""committer"":{""name"":""Christian Hoffmeister"",""email"":""thekwasti@googlemail.com"",""username"":""choffmeister""},""added"":[],""removed"":[],""modified"":["".core-ci.yml""]}],""head_commit"":{""id"":""d231ce397c6fd3b2c853ed1ffce0f3fc1bc3d192"",""distinct"":true,""message"":""Update .core-ci.yml"",""timestamp"":""2013-09-03T08:20:27-07:00"",""url"":""https://github.com/choffmeister/hook-test/commit/d231ce397c6fd3b2c853ed1ffce0f3fc1bc3d192"",""author"":{""name"":""Christian Hoffmeister"",""email"":""thekwasti@googlemail.com"",""username"":""choffmeister""},""committer"":{""name"":""Christian Hoffmeister"",""email"":""thekwasti@googlemail.com"",""username"":""choffmeister""},""added"":[],""removed"":[],""modified"":["".core-ci.yml""]},""repository"":{""id"":12490674,""name"":""hook-test"",""url"":""https://github.com/choffmeister/hook-test"",""description"":""Simple repository to test the GitHub hook."",""watchers"":0,""stargazers"":0,""forks"":0,""fork"":false,""size"":224,""owner"":{""name"":""choffmeister"",""email"":""mail@choffmeister.de""},""private"":false,""open_issues"":0,""has_issues"":true,""has_downloads"":true,""has_wiki"":true,""created_at"":1377879914,""pushed_at"":1378221628,""master_branch"":""master""},""pusher"":{""name"":""none""}}";

        [Test]
        public void TestHook()
        {
            GitHubConnector connector = new GitHubConnector(new MockConfigurationProvider(), this.UserRepository, this.ConnectorRepository, this.ProjectRepository, this.TaskRepository);

            this.ProjectRepository.Insert(new ProjectEntity()
            {
                Id = Guid.NewGuid(),
                Name = "hook-test",
                FullName = "choffmeister/hook-test",
                IsPrivate = false,
                Token = "123456"
            });

            MockHttpRequest request = new MockHttpRequest();
            request.FormData.Set("payload", _payload);
            request.QueryString.Add("token", "123456");

            connector.ProcessHook(request);

            Assert.AreEqual(1, this.ProjectRepository.Count());
            ProjectEntity project = this.ProjectRepository.Single();
            Assert.AreEqual("hook-test", project.Name);
            Assert.AreEqual("choffmeister/hook-test", project.FullName);

            Assert.AreEqual(1, this.TaskRepository.Count());
            TaskEntity task = this.TaskRepository.Single();
            Assert.AreEqual("master", task.Branch);
            Assert.AreEqual("precise64-mono", task.Configuration.Machine);
            Assert.AreEqual("sudo apt-get update\nid\necho Hello World\n", task.Configuration.TestScript);

            connector.ProcessHook(request);

            Assert.AreEqual(1, this.ProjectRepository.Count());
            Assert.AreEqual(2, this.TaskRepository.Count());
        }
    }
}
