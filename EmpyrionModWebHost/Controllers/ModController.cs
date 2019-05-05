using Eleon.Modding;
using EmpyrionNetAPIAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Linq;
using System;
using System.Threading;
using System.Diagnostics;
using EmpyrionModWebHost.Extensions;
using Microsoft.AspNetCore.SignalR;
using EmpyrionModWebHost.Models;
using EmpyrionNetAPITools;
using EmpyrionModWebHost.Services;

namespace EmpyrionModWebHost.Controllers
{
    [Authorize]
    public class ModinfoHub : Hub
    {
    }


    public class ModManager : EmpyrionModBase, IEWAPlugin
    {
        public static string StopFileName = Path.Combine(EmpyrionConfiguration.ProgramPath, "Content", "Mods", "ModLoader", "Client", "stop.txt");

        public IHubContext<ModinfoHub> ModinfoHub { get; }
        public ModGameAPI GameAPI { get; private set; }
        public Lazy<SysteminfoManager> SysteminfoManager { get; }
        public bool LastModsStatusState { get; private set; }
        public bool CheckModHostStatusStarted { get; private set; }

        public ModManager(IHubContext<ModinfoHub> aModinfoHub)
        {
            ModinfoHub        = aModinfoHub;
            SysteminfoManager = new Lazy<SysteminfoManager>(() => Program.GetManager<SysteminfoManager>());
        }

        private void CheckModHostStatus()
        {
            var CurrentState = ModsStarted();
            if(LastModsStatusState != CurrentState)
            {
                LastModsStatusState = CurrentState;
                ModinfoHub?.Clients.All.SendAsync("ModHostRunning", LastModsStatusState);
            }
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
        }

        public bool ModsStarted()
        {
            if (!CheckModHostStatusStarted)
            {
                CheckModHostStatusStarted = true;
                TaskTools.Intervall(1000, CheckModHostStatus);
            }

            Process EGSProcess = null;
            try {
                if (SysteminfoManager.Value.ProcessInformation == null) return false;
                EGSProcess = Process.GetProcessById(SysteminfoManager.Value.ProcessInformation.Id);

                var AllChilds = EGSProcess?.GetChildProcesses().ToList();

                // Fallback falls die Processinfos nicht existieren
                if (AllChilds == null || AllChilds.Count == 0) return !File.Exists(StopFileName);

                var ESGChildProcesses = AllChilds.Where(P => P.ProcessName.StartsWith("EmpyrionModHost", StringComparison.InvariantCultureIgnoreCase)).ToArray();

                return ESGChildProcesses?.FirstOrDefault() != null;
            }
            catch
            {
                // Fallback falls die Processinfos nicht existieren
                return !File.Exists(StopFileName);
            }
        }
    }

    [ApiController]
    [Authorize(Roles = nameof(Role.ServerAdmin))]
    [Route("[controller]")]
    public class ModController : ControllerBase
    {
        public const string  ModsInstallPath = @"..\MODs\";
        public static string ModLoaderHostPath = Path.Combine(EmpyrionConfiguration.ProgramPath, "Content", "Mods", "ModLoader", "Host");
        public static string DllNamesFile = Path.Combine(ModLoaderHostPath, "DllNames.txt");

        public ModManager ModManager { get; }

        public ModController(IHubContext<ModinfoHub> aModinfoHub)
        {
            ModManager        = Program.GetManager<ModManager>();
        }

        [HttpGet("ModLoaderInstalled")]
        public ActionResult<string> ModLoaderInstalled()
        {
            var ModHostExe = Path.Combine(ModLoaderHostPath, "EmpyrionModHost.exe");
            return System.IO.File.Exists(ModHostExe)
                ? "\"" + ReadDllInfos(ModHostExe) + "\""
                : null;
        }

        [HttpGet("InstallModLoader")]
        public ActionResult<bool> InstallModLoader()
        {
            if (System.IO.File.Exists(DllNamesFile)) System.IO.File.Copy(DllNamesFile, DllNamesFile + ".bak", true);

            try { Directory.Delete(Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location), "PublishAddOns", "Temp"), true); } catch { }

            ZipFile.ExtractToDirectory(
                Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location), "PublishAddOns", "ModLoader.zip"),
                Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location), "PublishAddOns", "Temp"),
                true);

            BackupManager.CopyAll(
                new DirectoryInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location), "PublishAddOns", "Temp")),
                new DirectoryInfo(Path.Combine(EmpyrionConfiguration.ProgramPath, "Content", "Mods"))
                );

            try { Directory.Delete(Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location), "PublishAddOns", "Temp"), true); } catch { }

            if (System.IO.File.Exists(DllNamesFile + ".bak")) System.IO.File.Copy(DllNamesFile + ".bak", DllNamesFile, true);
            else                                              System.IO.File.WriteAllText(DllNamesFile, "");

            return System.IO.File.Exists(Path.Combine(ModLoaderHostPath, "EmpyrionModHost.exe"));
        }

        [HttpGet("DeleteAllMods")]
        public ActionResult<bool> DeleteAllMods()
        {
            Directory.Delete(Path.Combine(EmpyrionConfiguration.ProgramPath, "Content", "Mods", "ModLoader"), true);
            return Ok();
        }


        public class ModData
        {
            public string name { get; set; }
            public string[] possibleNames { get; set; }
            public bool active { get; set; }
            public string infos { get; set; }
            public bool withConfiguration { get; set; }
            public string configurationType { get; internal set; }
        }

        [HttpGet("ModInfos")]
        public ActionResult<ModData[]> ModInfos()
        {
            return System.IO.File.ReadAllLines(DllNamesFile)
                .Where(L => !string.IsNullOrWhiteSpace(L))
                .Select(L =>
                {
                    var ModDll = L.Length > ModsInstallPath.Length ? L.Substring(ModsInstallPath.Length + (L.StartsWith("#") ? 1 : 0)) : L;
                    var modConfig = GetModAppConfig(ModDll);

                    return new ModData()
                    {
                        active            = !L.StartsWith("#"),
                        name              = ModDll,
                        possibleNames     = ReadPossibleDLLs(ModDll),
                        infos             = ReadDllInfos(Path.Combine(ModLoaderHostPath, ModsInstallPath, ModDll)),
                        withConfiguration = modConfig != null,
                        configurationType = modConfig != null ? Path.GetExtension(modConfig.Current.ModConfigFile).Substring(1).ToLower() : null,
                    };
                }).ToArray();
        }

        private static string[] ReadPossibleDLLs(string ModDll)
        {
            var DLLPath = Path.Combine(ModLoaderHostPath, ModsInstallPath, Path.GetDirectoryName(ModDll));

            return Directory.Exists(DLLPath) 
                ? Directory.GetFiles(DLLPath, "*.dll")
                                    .Select(D => Path.Combine(Path.GetFileName(Path.GetDirectoryName(D)), Path.GetFileName(D)))
                                    .ToArray()
                : null;
        }

        private string ReadDllInfos(string aDLL)
        {
            if (!System.IO.File.Exists(aDLL)) return $"File not found: {aDLL}";

            var i = FileVersionInfo.GetVersionInfo(aDLL);
            return $"{i.CompanyName} Version:{i.FileVersion} {i.LegalCopyright}";
        }

        [HttpPost("ModInfos")]
        public IActionResult ModInfos([FromBody]ModData[] aModData)
        {
            System.IO.File.WriteAllLines(DllNamesFile,
                aModData.Select(M => $"{(M.active ? ModsInstallPath : "#" + ModsInstallPath)}{M.name}"));

            return Ok();
        }

        [HttpPost("DeleteMod")]
        public IActionResult DeleteMod([FromBody]ModData aModData)
        {
            var ModDirName = Path.Combine(ModLoaderHostPath, ModsInstallPath, Path.GetDirectoryName(aModData.name));
            if(Directory.Exists(ModDirName)) Directory.Delete(ModDirName, true);

            System.IO.File.WriteAllLines(DllNamesFile,
                System.IO.File.ReadAllLines(DllNamesFile)
                .Where(L => !L.Contains(aModData.name)).ToArray());

            return Ok();
        }

        [HttpGet("ModsStarted")]
        public ActionResult<bool> ModsStarted()
        {
            return ModManager.ModsStarted();
        }

        [HttpGet("StartMods")]
        public IActionResult StartMods()
        {
            if (System.IO.File.Exists(ModManager.StopFileName)) System.IO.File.Delete(ModManager.StopFileName);
            return Ok();
        }

        [HttpGet("StopMods")]
        public IActionResult StopMods()
        {
            System.IO.File.WriteAllText(ModManager.StopFileName, "stopped");
            return Ok();
        }

        [HttpPost("UploadFile")]
        [DisableRequestSizeLimit]
        public IActionResult UploadFile()
        {
            Program.CreateTempPath();

            foreach (var file in Request.Form.Files)
            {
                try { Directory.Delete(Path.Combine(ModLoaderHostPath, "Temp"), true);    } catch { }
                Thread.Sleep(1000);
                try { Directory.CreateDirectory(Path.Combine(ModLoaderHostPath, "Temp")); } catch { }

                var TargetFile = Path.Combine(ModLoaderHostPath, "Temp", file.Name);
                using (var ToFile = System.IO.File.Create(TargetFile))
                {
                    file.OpenReadStream().CopyTo(ToFile);
                }

                switch (Path.GetExtension(TargetFile).ToLower())
                {
                    case ".zip": InstallZipFile(TargetFile); break;
                    case ".dll": InstallDLLFile(TargetFile); break;
                }

                Directory.Delete(Path.Combine(ModLoaderHostPath, "Temp"), true);
            }
            return Ok(); 
        }

        public class ModAppConfig
        {
            public string ModConfigFile { get; set; }
        }

        [HttpGet("GetModConfig/{modDllName}")]
        public IActionResult GetModConfig(string modDllName)
        {
            var modConfig = GetModAppConfig(modDllName);
            if (modConfig == null) return NotFound();

            var modConfigFile = Path.Combine(EmpyrionConfiguration.SaveGameModPath, modConfig.Current.ModConfigFile);
            if (!System.IO.File.Exists(modConfigFile)) return NotFound();


            DateTimeOffset? LastModified = new DateTimeOffset(System.IO.File.GetLastWriteTime(modConfigFile));

            return PhysicalFile(
                modConfigFile,
                "application/" + Path.GetExtension(modConfigFile).Substring(1).ToLower(),
                Path.GetFileName(modConfigFile),
                LastModified,
                new Microsoft.Net.Http.Headers.EntityTagHeaderValue("\"" + ETagGenerator.GetETag(modConfigFile, System.IO.File.ReadAllBytes(modConfigFile)) + "\""),
                true
                );
        }

        [HttpPost("UploadModConfigFile/{modDllName}")]
        [DisableRequestSizeLimit]
        public IActionResult UploadModConfigFile(string modDllName)
        {
            Program.CreateTempPath();

            foreach (var file in Request.Form.Files)
            {
                try { Directory.Delete(Path.Combine(ModLoaderHostPath, "Temp"), true); } catch { }
                Thread.Sleep(1000);
                try { Directory.CreateDirectory(Path.Combine(ModLoaderHostPath, "Temp")); } catch { }

                var TargetFile = Path.Combine(ModLoaderHostPath, "Temp", file.Name);
                using (var ToFile = System.IO.File.Create(TargetFile))
                {
                    file.OpenReadStream().CopyTo(ToFile);
                }

                var modConfig = GetModAppConfig(modDllName);
                if (modConfig != null) System.IO.File.Copy(TargetFile, Path.Combine(EmpyrionConfiguration.SaveGameModPath, modConfig.Current.ModConfigFile));

                Directory.Delete(Path.Combine(ModLoaderHostPath, "Temp"), true);
            }

            return Ok();
        }

        [HttpPost("UploadModConfig/{modDllName}")]
        [DisableRequestSizeLimit]
        public IActionResult UploadModConfig(string modDllName)
        {
            foreach (var key in Request.Form.Keys)
            {
                var modConfig = GetModAppConfig(modDllName);
                if (modConfig != null) System.IO.File.WriteAllText(Path.Combine(EmpyrionConfiguration.SaveGameModPath, modConfig.Current.ModConfigFile), Request.Form[key]);
            }
            return Ok();
        }

        public static ConfigurationManager<ModAppConfig> GetModAppConfig(string modDllPath)
        {
            var ModDirName       = Path.Combine(ModLoaderHostPath, ModsInstallPath, Path.GetDirectoryName(modDllPath));
            var ModAppConfigFile = Path.Combine(ModDirName, "ModAppConfig.json");
            if (!Directory.Exists(ModDirName) || !System.IO.File.Exists(ModAppConfigFile)) return null;

            var ModAppConfig = new ConfigurationManager<ModAppConfig>() { ConfigFilename = ModAppConfigFile };
            ModAppConfig.Load();
            return ModAppConfig;
           
        }

        private void InstallDLLFile(string aDllFile)
        {
            var TargetDir = Path.Combine(ModLoaderHostPath, ModsInstallPath, Path.GetFileNameWithoutExtension(aDllFile));
            try { Directory.CreateDirectory(TargetDir); } catch { }

            var TargetFile = Path.Combine(TargetDir, Path.GetFileName(aDllFile));
            if (System.IO.File.Exists(TargetFile)) System.IO.File.Delete(TargetFile);
            System.IO.File.Move(aDllFile, TargetFile);

            AddToDllNamesIfNotExists(Path.Combine(Path.GetFileNameWithoutExtension(aDllFile), Path.GetFileName(aDllFile)));
        }

        private void AddToDllNamesIfNotExists(string aDllName)
        {
            var lines = System.IO.File.ReadAllLines(DllNamesFile).ToList();
            if (lines.Any(L => L.Contains(aDllName))) return;

            lines.Add($"#{ModsInstallPath}{aDllName}");

            System.IO.File.WriteAllLines(DllNamesFile, lines);
        }

        private void InstallZipFile(string aZipFile)
        {
            ZipFile.ExtractToDirectory(aZipFile, Path.GetDirectoryName(aZipFile));
            System.IO.File.Delete(aZipFile);

            // only single file
            var Files = Directory.EnumerateFiles(Path.GetDirectoryName(aZipFile)).ToArray();
            if (Files.Length == 1)
            {
                InstallDLLFile(Files.First());
                return;
            }

            var TargetDir = Path.Combine(ModLoaderHostPath, ModsInstallPath, Path.GetFileNameWithoutExtension(aZipFile));
            var SourceDir = Path.Combine(Path.GetDirectoryName(aZipFile), Path.GetFileNameWithoutExtension(aZipFile));
            if (!Directory.Exists(SourceDir)) SourceDir = Path.GetDirectoryName(aZipFile);

            // only sub directory
            var Dirs = Directory.EnumerateDirectories(SourceDir).ToArray();
               Files = Directory.EnumerateFiles(SourceDir).ToArray();
            if (Dirs.Length == 1 && Files.Length == 0)
            {
                SourceDir = Path.Combine(SourceDir, Dirs.First());
                TargetDir = Path.Combine(ModLoaderHostPath, ModsInstallPath, Path.GetFileName(Dirs.First()));
            }

            try { Directory.CreateDirectory(TargetDir); } catch { }
            BackupManager.CopyAll(new DirectoryInfo(SourceDir), new DirectoryInfo(TargetDir));

            AddToDllNamesIfNotExists(Path.Combine(Path.GetFileNameWithoutExtension(TargetDir), GetFirstModDLL(TargetDir)));
        }

        private string GetFirstModDLL(string aTargetDir)
        {
            return Directory.EnumerateFiles(aTargetDir)
                .Select(D => Path.GetFileName(D))
                .Where(D => Path.GetExtension(D).ToLower() == ".dll")
                .FirstOrDefault() ?? "???";
        }

    }

}