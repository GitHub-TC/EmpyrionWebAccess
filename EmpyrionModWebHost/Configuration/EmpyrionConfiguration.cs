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
                                                                            : GetDirWith(Directory.GetCurrentDirectory(), "BuildNumber.txt");
        public static string ModPath { get; private set; } = Path.Combine(ProgramPath, @"Content\Mods");
        public static string DedicatedFilename { get; private set; } = Environment.GetCommandLineArgs().Contains("-dedicated")
                                                                            ? Environment.GetCommandLineArgs().SkipWhile(A => string.Compare(A, "-dedicated", StringComparison.InvariantCultureIgnoreCase) != 0).Skip(1).FirstOrDefault()
                                                                            : "dedicated.yaml";

        public static string GetDirWith(string aTestDir, string aTestFile)
        {
            return File.Exists(Path.Combine(aTestDir, aTestFile))
                ? aTestDir
                : GetDirWith(Path.GetDirectoryName(aTestDir), aTestFile);
        }

        public static string SaveGamePath
        {
            get { return Path.Combine(ProgramPath, DedicatedYaml.SaveDirectory ?? "Saves", "Games", DedicatedYaml.SaveGameName ?? "");  }
        }

        public static string SaveGameCachePath
        {
            get { return Path.Combine(ProgramPath, DedicatedYaml.SaveDirectory ?? "Saves", "Cache", DedicatedYaml.SaveGameName ?? ""); }
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

                    var ServerConfigNode = Root.GetChild("ServerConfig") as YamlMappingNode;

                    ServerName          = ServerConfigNode.GetChild("Srv_Name")?.ToString();
                    SaveDirectory       = ServerConfigNode.GetChild("SaveDirectory")?.ToString();

                    var GameConfigNode  = Root.GetChild("GameConfig") as YamlMappingNode;

                    SaveGameName        = GameConfigNode.GetChild("GameName")?.ToString();
                    CustomScenarioName  = GameConfigNode.GetChild("CustomScenario")?.ToString();
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

                    var ElevatedNode = (Root.GetChild("Elevated") as YamlSequenceNode)?.Children;

                    ElevatedUsers = ElevatedNode?.OfType<YamlMappingNode>().Select(N =>
                    {
                        return new ElevatedUserStruct()
                        {
                            SteamId = N.Children[new YamlScalarNode("Id")]?.ToString(),
                            Name = N.GetChild("Name")?.ToString(),
                            Permission = int.TryParse(N.GetChild("Permission")?.ToString(), out int Result) ? Result : 0,
                        };
                    }).ToArray();

                    var BannedNode = (Root.GetChild("Banned") as YamlSequenceNode)?.Children;

                    BannedUsers = BannedNode?.OfType<YamlMappingNode>().Select(N =>
                    {
                        return new BannedUserStruct()
                        {
                            SteamId = N.GetChild("Id")?.ToString(),
                            Until = DateTime.TryParse(N.GetChild("Until")?.ToString(), out DateTime Result) ? Result : DateTime.MinValue,
                        };
                    }).ToArray();
                }
            }
        }
    }
}
