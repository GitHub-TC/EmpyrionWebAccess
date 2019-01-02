using Eleon.Modding;
using EmpyrionNetAPIAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Linq;
using System;
using System.Globalization;
using System.Threading;
using System.Diagnostics;
using EmpyrionModWebHost.Extensions;
using Microsoft.AspNetCore.SignalR;

namespace EmpyrionModWebHost.Controllers
{
    [Authorize]
    public class ModinfoHub : Hub
    {
    }


    public class ModManager : EmpyrionModBase, IEWAPlugin
    {
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
            try { EGSProcess = Process.GetProcessById(SysteminfoManager.Value.ProcessInformation.Id); } catch { }
            var ESGChildProcesses = EGSProcess?.GetChildProcesses().Where(P => P.ProcessName == "EmpyrionModHost").ToArray();

            return ESGChildProcesses?.FirstOrDefault() != null;
        }
    }

    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class ModController : ControllerBase
    {
        public const string  ModsInstallPath = @"..\MODs\";
        public static string StopFileName = Path.Combine(EmpyrionConfiguration.ProgramPath, "Content", "Mods", "ModLoader", "Client", "stop.txt");
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

            ZipFile.ExtractToDirectory(
                Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(GetType()).Location), "PublishAddOns", "ModLoader.zip"),
                Path.Combine(EmpyrionConfiguration.ProgramPath, "Content", "Mods", "ModLoader"),
                true);

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

        }

        [HttpGet("ModInfos")]
        public ActionResult<ModData[]> ModInfos()
        {
            return System.IO.File.ReadAllLines(DllNamesFile)
                .Where(L => !string.IsNullOrWhiteSpace(L))
                .Select(L =>
                {
                    var ModDll = L.Length > ModsInstallPath.Length ? L.Substring(ModsInstallPath.Length + (L.StartsWith("#") ? 1 : 0)) : L;

                    return new ModData()
                    {
                        active          = !L.StartsWith("#"),
                        name            = ModDll,
                        possibleNames   = ReadPossibleDLLs(ModDll),
                        infos           = ReadDllInfos(Path.Combine(ModLoaderHostPath, ModsInstallPath, ModDll))
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
            if (System.IO.File.Exists(StopFileName)) System.IO.File.Delete(StopFileName);
            return Ok();
        }

        [HttpGet("StopMods")]
        public IActionResult StopMods()
        {
            System.IO.File.WriteAllText(StopFileName, "stopped");
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
           if(System.IO.File.ReadAllLines(DllNamesFile).Any(L => L.Contains(aDllName))) return;

           System.IO.File.AppendAllLines(DllNamesFile,new[] { $"#{ModsInstallPath}{aDllName}" });
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
                .FirstOrDefault() ?? "???";
        }

    }

}