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
using YamlDotNet.RepresentationModel;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace CoreCI.Common
{
    public class YamlConfigurationProvider : IConfigurationProvider
    {
        private readonly string _path;
        private readonly YamlMappingNode _yaml;

        public static YamlConfigurationProvider Default
        {
            get
            {
                string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string configurationPath = Path.Combine(homePath, ".core-ci.conf.yml");

                return new YamlConfigurationProvider(configurationPath);
            }
        }

        public YamlConfigurationProvider(string path)
        {
            _path = path;
            _yaml = ParseYaml(_path);
        }

        public string Get(string name, bool throwIfNotExistent = true)
        {
            YamlScalarNode node = (YamlScalarNode)Traverse(_yaml, name);

            return node.Value;
        }

        public string[] GetArray(string name, bool throwIfNotExistent = true)
        {
            YamlSequenceNode node = (YamlSequenceNode)Traverse(_yaml, name);

            return node.Children
                .Select(n => ((YamlScalarNode)n).Value)
                .ToArray();
        }

        public void Dispose()
        {
        }

        private static YamlNode Traverse(YamlNode current, string name)
        {
            string[] parts = name.Split(new char[] { '.' }, StringSplitOptions.None);

            foreach (string part in parts)
            {
                current = ((YamlMappingNode)current).Children [new YamlScalarNode(part)];
            }

            return current;
        }

        private static YamlMappingNode ParseYaml(string path)
        {
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var fileReader = new StreamReader(file))
            {
                YamlStream yaml = new YamlStream();
                yaml.Load(fileReader);

                return (YamlMappingNode)yaml.Documents [0].RootNode;
            }
        }
    }
}
