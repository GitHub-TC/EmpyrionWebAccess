using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionModWebHost.Models;
using EmpyrionNetAPIAccess;
using EmpyrionNetAPIDefinitions;
using EmpyrionNetAPITools;
using EWAExtenderCommunication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;

namespace EmpyrionModWebHost.Controllers
{

#pragma warning disable IDE1006 // Naming Styles
    public class SystemInfoDataModel
    {
        /// <summary>
        /// Onlinestates:
        /// o: EGS Online
        /// c: EGS CommError
        /// E: EGS Down
        /// b: Backup
        /// r: Restart
        /// S: EGS Stop/Start
        /// D: Client disconnect (used by Client)
        /// </summary>
        public string online { get; set; }
        public string copyright { get; set; }
        public string version { get; set; }
        public string versionESG { get; set; }
        public string versionESGBuild { get; set; }
        public int activePlayers { get; set; }
        public int activePlayfields { get; set; }
        public int totalPlayfieldserver { get; set; }
        public long totalPlayfieldserverMemorySize { get; set; }
        public long diskFreeSpace { get; set; }
        public long diskUsedSpace { get; set; }
        public int cpuTotalLoad { get; set; }
        public int ramAvailableMB { get; set; }
        public ulong ramTotalMB { get; set; }
        public string serverName { get; set; }
        public bool eahAvailable { get; set; }
        public long ewaMemorySize { get; set; }
    }
#pragma warning restore IDE1006 // Naming Styles

    public class SystemConfig
    {
        public ProcessInformation ProcessInformation { get; set; }
        public string StartCMD { get; set; }
        public string WelcomeMessage { get; set; }
        public string PlayerSteamInfoUrl { get; set; }

    }

    [Authorize]
    public class SysteminfoHub : Hub
    {
    }

    public class SysteminfoManager : EmpyrionModBase, IEWAPlugin, IClientHostCommunication
    {
        public SystemInfoDataModel CurrentSysteminfo = new SystemInfoDataModel();

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        public ulong InstalledTotalMemory()
        {
            MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
            return GlobalMemoryStatusEx(memStatus) ? memStatus.ullTotalPhys : 0;
        }


        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        public IHubContext<SysteminfoHub> SysteminfoHub { get; internal set; }
        public PlayerManager PlayerManager { get; private set; }
        public ClientMessagePipe ToEmpyrion { get; set; }
        public ModGameAPI GameAPI { get; private set; }
        public DateTime LastProcessInformationUpdate { get; private set; } = DateTime.Now;
        public ProcessInformation ProcessInformation { get; private set; }
        public PerformanceCounter CpuTotalLoad { get; private set; }
        public PerformanceCounter RamAvailable { get; private set; }
        public ConfigurationManager<SystemConfig> SystemConfig { get; private set; }

        public ILogger<SysteminfoManager> Logger { get; set; }
        public string EWAUpdateDir { get; internal set; } = Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "Update");

        public SysteminfoManager(IHubContext<SysteminfoHub> aSysteminfoHub, ILogger<SysteminfoManager> aLogger)
        {
            Logger = aLogger;
            SysteminfoHub = aSysteminfoHub;

            SystemConfig = new ConfigurationManager<SystemConfig>
            {
                ConfigFilename = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "EWA", "SystemConfig.xml")
            };

            SystemConfig.Load();

            if (string.IsNullOrEmpty(SystemConfig.Current.PlayerSteamInfoUrl)) SystemConfig.Current.PlayerSteamInfoUrl = "https://steamcommunity.com/profiles";
        }

        private void UpdateSystemInfo()
        {
            SysteminfoHub?.Clients.All.SendAsync("Update", JsonConvert.SerializeObject(CurrentSysteminfo)).Wait(1000);
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            LogLevel = EmpyrionNetAPIDefinitions.LogLevel.Debug;

            PlayerManager = Program.GetManager<PlayerManager>();

            TaskTools.Intervall(2000,  () => SysteminfoHub?.Clients.All.SendAsync("UPC", JsonConvert.SerializeObject(new {
                o       = CurrentSysteminfo.online,
                ap      = CurrentSysteminfo.activePlayers,
                apf     = CurrentSysteminfo.activePlayfields,
                c       = CurrentSysteminfo.cpuTotalLoad,
                r       = CurrentSysteminfo.ramAvailableMB,
                tpf     = CurrentSysteminfo.totalPlayfieldserver,
                tpfm    = CurrentSysteminfo.totalPlayfieldserverMemorySize,
                ewam    = CurrentSysteminfo.ewaMemorySize,
            })).Wait(1000));
            TaskTools.Intervall(30000, UpdateSystemInfo);
            TaskTools.Intervall(5000,  UpdateEmpyrionInfos);
            TaskTools.Intervall(5000,  UpdateComputerInfos);
            TaskTools.Intervall(2000,  UpdatePerformanceInfos);

            try
            {
                CpuTotalLoad = new PerformanceCounter
                {
                    CategoryName = "Processor",
                    CounterName = "% Processor Time",
                    InstanceName = "_Total"
                };

                RamAvailable = new PerformanceCounter("Memory", "Available MBytes");
            }
            catch (Exception error)
            {
                Logger.LogError(error, $"User not in the 'Performance Monitor Users' group, no performance counter available");
            }
        }

        public bool EGSIsRunning => CurrentSysteminfo.online == "o";

        private void UpdatePerformanceInfos()
        {
            CurrentSysteminfo.online = SetState(CurrentSysteminfo.online, "oc", (DateTime.Now - LastProcessInformationUpdate).TotalSeconds <= 10);

            var GameDrive = DriveInfo.GetDrives().FirstOrDefault(D => D.RootDirectory.FullName == Path.GetPathRoot(ProcessInformation == null ? Directory.GetCurrentDirectory() : ProcessInformation.CurrentDirecrory));

            ulong memBytes = InstalledTotalMemory();

            CurrentSysteminfo.cpuTotalLoad      = (int)(CpuTotalLoad?.NextValue() ?? 0);
            CurrentSysteminfo.ramAvailableMB    = (int)(RamAvailable?.NextValue() ?? 0);
            CurrentSysteminfo.ramTotalMB        = memBytes / (1024 * 1024);
            CurrentSysteminfo.diskUsedSpace     = GameDrive.TotalSize - GameDrive.TotalFreeSpace;
            CurrentSysteminfo.diskFreeSpace     = GameDrive.TotalFreeSpace;

            CurrentSysteminfo.ewaMemorySize     = Process.GetCurrentProcess().PrivateMemorySize64;
        }

        public string SetState(string aState, string aStateChar, bool aStateSet)
        {
            return (aState ?? string.Empty)
                .Where(C => !aStateChar.Contains(C))
                .Aggregate("", (c, s) => c + s) +
                (aStateSet ? "" + aStateChar[0] : (aStateChar.Length > 1 ? "" + aStateChar[1] : ""));
        }

        private void UpdateEmpyrionInfos()
        {
            CurrentSysteminfo.online = SetState(CurrentSysteminfo.online, "oc", (DateTime.Now - LastProcessInformationUpdate).TotalSeconds <= 10);

            if (ToEmpyrion == null) return;

            ToEmpyrion.SendMessage(new ClientHostComData() { Command = ClientHostCommand.ProcessInformation });
            if (ProcessInformation == null) return;

            CurrentSysteminfo.activePlayers    = PlayerManager.OnlinePlayersCount;
            var activePlayfields               = Request_Playfield_List().Result.playfields;
            CurrentSysteminfo.activePlayfields = activePlayfields == null ? 0 : activePlayfields.Count;

            Process EGSProcess = null;
            try{ EGSProcess = Process.GetProcessById(ProcessInformation.Id); } catch {}
            CurrentSysteminfo.online = SetState(CurrentSysteminfo.online, "E", EGSProcess == null);
            var ESGChildProcesses = EGSProcess?.GetChildProcesses().Where(P => P.ProcessName == "EmpyrionPlayfieldServer").ToArray();

            if (ESGChildProcesses != null)
            {
                CurrentSysteminfo.totalPlayfieldserver           = ESGChildProcesses.Count();
                CurrentSysteminfo.totalPlayfieldserverMemorySize = ESGChildProcesses.Aggregate(0L, (S, P) => S + P.PrivateMemorySize64);
            }

            var eahProcess = Process.GetProcessesByName("EmpAdminHelper").FirstOrDefault();
            CurrentSysteminfo.eahAvailable = eahProcess != null;

            SystemConfig.Current.ProcessInformation = ProcessInformation;
            SystemConfig.Save();
        }
        private void UpdateComputerInfos()
        {
            var CurrentAssembly = Assembly.GetAssembly(this.GetType());

            CurrentSysteminfo.serverName = EmpyrionConfiguration.DedicatedYaml.ServerName;
            CurrentSysteminfo.version = CurrentAssembly.GetAttribute<AssemblyFileVersionAttribute>()?.Version;
            CurrentSysteminfo.versionESG = EmpyrionConfiguration.Version;
            CurrentSysteminfo.versionESGBuild = EmpyrionConfiguration.BuildVersion;
            CurrentSysteminfo.copyright = CurrentAssembly.GetAttribute<AssemblyCopyrightAttribute>()?.Copyright;
        }

        public void ClientHostMessage(ClientHostComData aMessage)
        {
            switch (aMessage.Command)
            {
                case ClientHostCommand.ProcessInformation:
                    LastProcessInformationUpdate = DateTime.Now;
                    if(aMessage.Data == null) {
                        ToEmpyrion.SendMessage(new ClientHostComData()
                        {
                            Command = ClientHostCommand.ProcessInformation,
                            Data = new ProcessInformation() { Id = Process.GetCurrentProcess().Id }
                        });
                    }
                    else ProcessInformation = aMessage.Data as ProcessInformation; break;
            }
        }

        public void EGSRunState(bool aStopped)
        {
            CurrentSysteminfo.online = SetState(CurrentSysteminfo.online, "S", aStopped);
        }

        public void EGSStop(int aWaitMinutes)
        {
            try
            {
                Logger.Log(Microsoft.Extensions.Logging.LogLevel.Information, "EGSStop");
                EGSRunState(true);
                Program.Host.ExposeShutdownHost();

                var stoptime = DateTime.Now.AddMinutes(aWaitMinutes);
                var exit = aWaitMinutes == 0 ? null : TaskTools.Intervall(10000, () => {
                    Request_InGameMessage_AllPlayers($"Server shutdown in {(stoptime - DateTime.Now).ToString(@"mm\:ss")}".ToIdMsgPrio(0, MessagePriorityType.Alarm));
                });

                try
                {
                    Process EGSProcess = null;
                    try { EGSProcess = ProcessInformation == null ? null : Process.GetProcessById(ProcessInformation.Id); } catch { }

                    Logger.Log(Microsoft.Extensions.Logging.LogLevel.Information, "EGSStop: saveandexit:" + aWaitMinutes);

                    try{ Request_ConsoleCommand(new PString("saveandexit " + aWaitMinutes)); }
                    catch (Exception Error) { Logger.LogError(Error, "EGSStop: StopCMD:saveandexit " + aWaitMinutes); }

                    Thread.Sleep(10000);
                    if (EGSProcess != null && !EGSProcess.HasExited)
                    {
                        Logger.Log(Microsoft.Extensions.Logging.LogLevel.Information, "EGSStop: Wait:" + aWaitMinutes);
                        EGSProcess?.WaitForExit((aWaitMinutes + 1) * 60000);
                    }
                    exit?.Set();

                    CurrentSysteminfo.online = SetState(CurrentSysteminfo.online, "o", false);
                }
                catch (Exception Error)
                {
                    exit?.Set();
                    Logger.LogError(Error, "EGSStop: WaitForExit");
                    Thread.Sleep(10000);
                }

                UpdateClient();
            }
            catch (Exception Error)
            {
                Logger.LogError(Error, "EGSStop");
            }
        }

        public void EGSStart()
        {
            try
            {
                Logger.Log(Microsoft.Extensions.Logging.LogLevel.Information, "EGSStart");
                Process EGSProcess = null;
                try { EGSProcess = ProcessInformation == null ? null : Process.GetProcessById(ProcessInformation.Id); } catch { }

                if (EGSProcess != null && !EGSProcess.HasExited)
                {
                    EGSRunState(false);
                    return;
                }

                var StartCMD = string.IsNullOrEmpty(SystemConfig.Current.StartCMD) ? SystemConfig.Current.ProcessInformation?.FileName : SystemConfig.Current.StartCMD;

                EGSProcess = new Process
                {
                    StartInfo = new ProcessStartInfo(Path.Combine(EmpyrionConfiguration.ProgramPath, Path.GetFileName(StartCMD)))
                    {
                        UseShellExecute  = !string.IsNullOrEmpty(SystemConfig.Current.StartCMD),
                        WindowStyle      = ProcessWindowStyle.Normal,
#pragma warning disable CA1416 // Validate platform compatibility
                        LoadUserProfile  = true,
#pragma warning restore CA1416 // Validate platform compatibility
                        CreateNoWindow   = false,
                        WorkingDirectory = EmpyrionConfiguration.ProgramPath,
                        Arguments        = SystemConfig.Current.ProcessInformation?.Arguments,
                    }
                };

                EGSProcess.Start();
            }
            catch (Exception Error)
            {
                Logger.LogError(Error, "EGSStart");
            }
            EGSRunState(false);
        }

        public void UpdateClient()
        {
            if (CurrentSysteminfo.online.Contains('o')) return;
            Logger.Log(Microsoft.Extensions.Logging.LogLevel.Information, "UpdateClient");

            UpdateClientFiles();
            UpdateClientMain();
        }

        private void UpdateClientFiles()
        {
            if (!Directory.Exists(Path.Combine(EWAUpdateDir, "EWALoader", "Client"))) return;

            try
            {
                if (Directory.Exists(Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "Client.bak")))
                {
                    Directory.Delete(Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "Client.bak"), true);
                }

                Directory.Move(
                    Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "Client"),
                    Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "Client.bak")
                    );

                Directory.Move(
                    Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "Update", "EWALoader", "Client"),
                    Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "Client")
                    );

                File.Copy(
                    Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "Client.bak", "Configuration.xml"),
                    Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "Client", "Configuration.xml"),
                    true);
            }
            catch (Exception Error)
            {
                Logger.LogError(Error, "UpdateClient");
            }
        }

        private void UpdateClientMain()
        {
            if (!Directory.Exists(Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "Update", "EWALoader"))) return;
            if (Directory.GetFiles(Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "Update", "EWALoader")).Count() == 0) return;

            try
            {
                if (Directory.Exists(Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "Main.bak")))
                {
                    Directory.Delete(Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "Main.bak"), true);
                }
                Directory.CreateDirectory(Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "Main.bak"));

                Directory.GetFiles(Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader"))
                    .ForEach(F => File.Move(F, Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "Main.bak", Path.GetFileName(F))));

                Directory.GetFiles(Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", "Update", "EWALoader"))
                    .ForEach(F => File.Move(F, Path.Combine(EmpyrionConfiguration.ModPath, "EWALoader", Path.GetFileName(F))));
            }
            catch (Exception Error)
            {
                Logger.LogError(Error, "UpdateClient");
            }
        }

        public void UpdateEWA()
        {
            if (!CurrentSysteminfo.online.Contains('o')) return;

            ToEmpyrion.SendMessage(new ClientHostComData()
            {
                Command = ClientHostCommand.UpdateEWA,
                Data    = new ProcessInformation() { Id = Process.GetCurrentProcess().Id }
            });
        }

        public void EWARestart()
        {
            Logger.LogInformation("EWARestart");
            Program.Application.StopAsync();
        }
    }

    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class SysteminfoController : ControllerBase
    {
        public IHubContext<SysteminfoHub> SysteminfoHub { get; }
        public SysteminfoManager SysteminfoManager { get; }

        public SysteminfoController(IHubContext<SysteminfoHub> aSysteminfoHub)
        {
            SysteminfoHub = aSysteminfoHub;
            SysteminfoManager = Program.GetManager<SysteminfoManager>();
            SysteminfoManager.SysteminfoHub = aSysteminfoHub;
        }

        [HttpGet("CurrentSysteminfo")]
        public ActionResult<SystemInfoDataModel> GetCurrentSysteminfo()
        {
            return Ok(SysteminfoManager.CurrentSysteminfo);
        }

        [HttpGet("StartCMDs")]
        public ActionResult<string[]> StartCMDs()
        {
            return Ok(Directory.EnumerateFiles(EmpyrionConfiguration.ProgramPath, "*.cmd").Select(F => Path.GetFileName(F)).ToArray());
        }

        [HttpGet("EGSStart")]
        [Authorize(Roles = nameof(Role.Moderator))]
        public IActionResult EGSStart()
        {
            SysteminfoManager.EGSStart();
            return Ok();
        }

        [HttpGet("EGSStop/{aWaitMinutes}")]
        [Authorize(Roles = nameof(Role.Moderator))]
        public IActionResult EGSStop(int aWaitMinutes)
        {
            SysteminfoManager.EGSStop(aWaitMinutes);
            return Ok();
        }

        
        [HttpGet("EGSRestart/{aWaitMinutes}")]
        [Authorize(Roles = nameof(Role.Moderator))]
        public IActionResult EGSRestart(int aWaitMinutes)
        {
            SysteminfoManager.EGSStop(aWaitMinutes);
            SysteminfoManager.EGSStart();
            return Ok();
        }

        [HttpGet("ShutdownEGSandEWA")]
        [Authorize(Roles = nameof(Role.ServerAdmin))]
        public IActionResult ShutdownEGSandEWA()
        {
            SysteminfoManager.EGSStop(0);
            new Thread(() => {try { Program.Host.HandleGameExit(true); } catch { } }).Start();
            return Ok();
        }

        [HttpGet("SystemConfig")]
        public ActionResult<SystemConfig> GetCurrentSystemConfig()
        {
            return Ok(SysteminfoManager.SystemConfig.Current);
        }

        [HttpPost("SystemConfig")]
        [Authorize(Roles = nameof(Role.ServerAdmin))]
        public IActionResult SetCurrentSystemConfig([FromBody] SystemConfig aSystemConfig)
        {
            var SaveInfos = SysteminfoManager.SystemConfig.Current.ProcessInformation;
            SysteminfoManager.SystemConfig.Current = aSystemConfig;
            SysteminfoManager.SystemConfig.Current.ProcessInformation = SaveInfos;
            SysteminfoManager.SystemConfig.Save();
            return Ok();
        }

        [HttpPost("UploadFile")]
        [Authorize(Roles = nameof(Role.ServerAdmin))]
        [DisableRequestSizeLimit]
        public IActionResult UploadFile()
        {
            Program.CreateTempPath();

            foreach (var file in Request.Form.Files)
            {
                try { Directory.Delete(SysteminfoManager.EWAUpdateDir, true); } catch { }
                Thread.Sleep(1000);
                try { Directory.CreateDirectory(SysteminfoManager.EWAUpdateDir); } catch { }

                var TargetFile = Path.Combine(SysteminfoManager.EWAUpdateDir, file.Name);
                using (var ToFile = System.IO.File.Create(TargetFile))
                {
                    file.OpenReadStream().CopyTo(ToFile);
                }

                ZipFile.ExtractToDirectory(TargetFile, SysteminfoManager.EWAUpdateDir);
                System.IO.File.Delete(TargetFile);
            }

            SysteminfoManager.UpdateEWA();

            return Ok();
        }

    }
}
