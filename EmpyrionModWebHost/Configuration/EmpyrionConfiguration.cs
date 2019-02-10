using EmpyrionModWebHost.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace EmpyrionModWebHost
{
    public class EmpyrionConfiguration
    {
        public static string ProgramPath { get; private set; } = Environment.GetCommandLineArgs().Contains("-GameDir")
                                                                            ? Environment.GetCommandLineArgs().SkipWhile(A => string.Compare(A, "-GameDir", StringComparison.InvariantCultureIgnoreCase) != 0).Skip(1).FirstOrDefault()
                                                                            : Directory.GetCurrentDirectory();
        public static string ModPath { get; private set; } = Path.Combine(ProgramPath, @"Content\Mods");
        public static string DedicatedFilename { get; private set; } = Environment.GetCommandLineArgs().Contains("-dedicated")
                                                                            ? Environment.GetCommandLineArgs().SkipWhile(A => string.Compare(A, "-dedicated", StringComparison.InvariantCultureIgnoreCase) != 0).Skip(1).FirstOrDefault()
                                                                            : "dedicated.yaml";

        public static string SaveGamePath
        {
            get { return Path.Combine(ProgramPath, DedicatedYaml.SaveDirectory ?? "", "Games", DedicatedYaml.SaveGameName ?? "");  }
        }

        public static string SaveGameModPath
        {
            get { return Path.Combine(SaveGamePath, @"Mods\EWA"); }
        }

        public static DedicatedYamlStruct DedicatedYaml { get; set; } = new DedicatedYamlStruct(Path.Combine(ProgramPath, DedicatedFilename));
        public static AdminconfigYamlStruct AdminconfigYaml { get; set; } = new AdminconfigYamlStruct(Path.Combine(ProgramPath, "Saves", "adminconfig.yaml"));
        public static string Version
        {
            get {
                try
                {
                    return File.ReadAllLines(Path.Combine(ProgramPath, "BuildNumber.txt"))
                        .Skip(1).FirstOrDefault()?
                        .Replace("\"", "").Replace(";", "")
                        .Trim();
                }
                catch
                {
                    return "???";
                }
            }
        }

        public static string BuildVersion
        {
            get {
                try
                {
                    return File.ReadAllLines(Path.Combine(ProgramPath, "BuildNumber.txt"))
                        .FirstOrDefault()?
                        .Trim();
                }
                catch
                {
                    return "???";
                }
            }
        }

        public class DedicatedYamlStruct
        {
            private FileSystemWatcher mDedicatedYamlFileWatcher;

            public string SaveGameName { get; private set; }
            public string CustomScenarioName { get; private set; }
            public string ServerName { get; private set; }
            public string SaveDirectory { get; private set; }

            public DedicatedYamlStruct(string aFilename)
            {
                if (!File.Exists(aFilename)) return;

                Load(aFilename);

                mDedicatedYamlFileWatcher = new FileSystemWatcher
                {
                    Path = Path.GetDirectoryName(aFilename),
                    NotifyFilter = NotifyFilters.LastWrite,
                    Filter = Path.GetFileName(aFilename)
                };
                mDedicatedYamlFileWatcher.Changed += (s, e) => TaskTools.Delay(10, () => Load(aFilename));
                mDedicatedYamlFileWatcher.EnableRaisingEvents = true;
            }

            private void Load(string aFilename)
            {
                using (var input = new StringReader(File.ReadAllText(aFilename)))
                {
                    var yaml = new YamlStream();
                    yaml.Load(input);

                    var Root = (YamlMappingNode)yaml.Documents[0].RootNode;

                    var ServerConfigNode = Root.Children[new YamlScalarNode("ServerConfig")] as YamlMappingNode;

                    ServerName = ServerConfigNode?.Children[new YamlScalarNode("Srv_Name")]?.ToString();
                    SaveDirectory = ServerConfigNode?.Children[new YamlScalarNode("SaveDirectory")]?.ToString();

                    var GameConfigNode = Root.Children[new YamlScalarNode("GameConfig")] as YamlMappingNode;

                    SaveGameName = GameConfigNode?.Children[new YamlScalarNode("GameName")]?.ToString();
                    CustomScenarioName = GameConfigNode?.Children[new YamlScalarNode("CustomScenario")]?.ToString();
                }
            }
        }

        public class AdminconfigYamlStruct
        {
            private FileSystemWatcher mAdminconfigYamlFileWatcher;

            public IEnumerable<ElevatedUserStruct> ElevatedUsers { get; private set; }
            public IEnumerable<BannedUserStruct> BannedUsers { get; private set; }

            public class ElevatedUserStruct
            {
                public string SteamId { get; set; }
                public string Name { get; set; }
                public int Permission { get; set; }
            }
            public class BannedUserStruct
            {
                public string SteamId { get; set; }
                public DateTime Until { get; set; }
            }

            public AdminconfigYamlStruct(string aFilename)
            {
                if (!File.Exists(aFilename)) return;

                Load(aFilename);

                mAdminconfigYamlFileWatcher = new FileSystemWatcher
                {
                    Path = Path.GetDirectoryName(aFilename),
                    NotifyFilter = NotifyFilters.LastWrite,
                    Filter = Path.GetFileName(aFilename)
                };
                mAdminconfigYamlFileWatcher.Changed += (s, e) => TaskTools.Delay(10, () => Load(aFilename));
                mAdminconfigYamlFileWatcher.EnableRaisingEvents = true;
            }

            private void Load(string aFilename)
            {
                using (var input = new StringReader(File.ReadAllText(aFilename)))
                {
                    var yaml = new YamlStream();
                    yaml.Load(input);

                    var Root = (YamlMappingNode)yaml.Documents[0].RootNode;

                    var ElevatedNode = (Root.Children[new YamlScalarNode("Elevated")] as YamlSequenceNode)?.Children;

                    ElevatedUsers = ElevatedNode?.OfType<YamlMappingNode>().Select(N =>
                    {
                        return new ElevatedUserStruct()
                        {
                            SteamId = N.Children[new YamlScalarNode("Id")]?.ToString(),
                            Name = GetValueOf(N, "Name"),
                            Permission = int.TryParse(N.Children[new YamlScalarNode("Permission")]?.ToString(), out int Result) ? Result : 0,
                        };
                    }).ToArray();

                    var BannedNode = (Root.Children[new YamlScalarNode("Banned")] as YamlSequenceNode)?.Children;

                    BannedUsers = BannedNode?.OfType<YamlMappingNode>().Select(N =>
                    {
                        return new BannedUserStruct()
                        {
                            SteamId = N.Children[new YamlScalarNode("Id")]?.ToString(),
                            Until = DateTime.TryParse(N.Children[new YamlScalarNode("Until")]?.ToString(), out DateTime Result) ? Result : DateTime.MinValue,
                        };
                    }).ToArray();
                }
            }

            private static string GetValueOf(YamlMappingNode aNode, string aKey)
            {
                var Found = aNode.Children.FirstOrDefault(P => P.Key.ToString() == aKey);
                return Found.Value?.ToString();
            }
        }
    }
}
