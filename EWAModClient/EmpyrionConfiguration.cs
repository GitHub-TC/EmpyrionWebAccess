using System;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace EWAModClient
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
            get { return Path.Combine(Path.Combine(
                Path.Combine(ProgramPath, DedicatedYaml.SaveDirectory ?? ""), "Games"), DedicatedYaml.SaveGameName ?? "");  }
        }

        public static string SaveGameModPath
        {
            get { return Path.Combine(SaveGamePath, @"Mods\EWA"); }
        }

        public static DedicatedYamlStruct DedicatedYaml { get; set; } = new DedicatedYamlStruct(Path.Combine(ProgramPath, DedicatedFilename));
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


        public class DedicatedYamlStruct
        {
            public string SaveGameName { get; }
            public string CustomScenarioName { get; }
            public string ServerName { get; }
            public string AdminConfigFilename { get; }
            public string SaveDirectory { get; }

            public DedicatedYamlStruct(string aFilename)
            {
                if (!File.Exists(aFilename)) return;

                using (var input = File.OpenText(aFilename))
                {
                    var yaml = new YamlStream();
                    yaml.Load(input);

                    var Root = (YamlMappingNode)yaml.Documents[0].RootNode;

                    var ServerConfigNode = Root.Children[new YamlScalarNode("ServerConfig")] as YamlMappingNode;

                    ServerName          = ServerConfigNode?.Children[new YamlScalarNode("Srv_Name")]?.ToString();
                    AdminConfigFilename = ServerConfigNode?.Children[new YamlScalarNode("AdminConfigFile")]?.ToString();
                    SaveDirectory       = ServerConfigNode?.Children[new YamlScalarNode("SaveDirectory")]?.ToString();

                    var GameConfigNode = Root.Children[new YamlScalarNode("GameConfig")] as YamlMappingNode;

                    SaveGameName       = GameConfigNode?.Children[new YamlScalarNode("GameName")]?.ToString();
                    CustomScenarioName = GameConfigNode?.Children[new YamlScalarNode("CustomScenario")]?.ToString();

                }

            }

        }
    }
}
