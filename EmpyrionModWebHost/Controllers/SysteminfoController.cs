﻿using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionNetAPIAccess;
using EWAExtenderCommunication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace EmpyrionModWebHost.Controllers
{

    public class SysteminfoDataModel
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
        public string online;
        public string copyright;
        public string version;
        public string versionESG;
        public int activePlayers;
        public int activePlayfields;
        public int totalPlayfieldserver;
        public long totalPlayfieldserverRamMB;
        public long diskFreeSpace;
        public long diskUsedSpace;
        public float cpuTotalLoad;
        public float ramAvailableMB;
        public long ramTotalMB;
        public string serverName;
    }

    public class SystemConfig
    {
        public ProcessInformation ProcessInformation { get; set; }
        public string StartCMD { get; set; }

    }

    [Authorize]
    public class SysteminfoHub : Hub
    {
        private SysteminfoManager SysteminfoManager { get; set; }
    }

    public class SysteminfoManager : EmpyrionModBase, IEWAPlugin, IClientHostCommunication
    {
        public SysteminfoDataModel CurrentSysteminfo = new SysteminfoDataModel();

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

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

        public SysteminfoManager(IHubContext<SysteminfoHub> aSysteminfoHub, ILogger<SysteminfoManager> aLogger)
        {
            Logger = aLogger;
            SysteminfoHub = aSysteminfoHub;

            SystemConfig = new ConfigurationManager<SystemConfig>
            {
                ConfigFilename = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "SystemConfig.xml")
            };
            SystemConfig.Load();
        }

        private void UpdateSystemInfo()
        {
            SysteminfoHub?.Clients.All.SendAsync("Update", JsonConvert.SerializeObject(CurrentSysteminfo)).Wait();
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
                tpfm    = CurrentSysteminfo.totalPlayfieldserverRamMB,
            })).Wait(1000));
            TaskTools.Intervall(30000, () => SysteminfoHub?.Clients.All.SendAsync("Update", JsonConvert.SerializeObject(CurrentSysteminfo)).Wait(1000));
            TaskTools.Intervall(5000, UpdateEmpyrionInfos);
            TaskTools.Intervall(5000, UpdateComputerInfos);
            TaskTools.Intervall(2000, UpdatePerformanceInfos);

            CpuTotalLoad = new PerformanceCounter
            {
                CategoryName = "Processor",
                CounterName = "% Processor Time",
                InstanceName = "_Total"
            };

            RamAvailable = new PerformanceCounter("Memory", "Available MBytes");
        }

        private void UpdatePerformanceInfos()
        {
            CurrentSysteminfo.online = SetState(CurrentSysteminfo.online, "oc", (DateTime.Now - LastProcessInformationUpdate).TotalSeconds <= 10);

            var GameDrive = DriveInfo.GetDrives().FirstOrDefault(D => D.RootDirectory.FullName == Path.GetPathRoot(ProcessInformation == null ? Directory.GetCurrentDirectory() : ProcessInformation.CurrentDirecrory));

            GetPhysicallyInstalledSystemMemory(out long memKb);

            CurrentSysteminfo.cpuTotalLoad = CpuTotalLoad.NextValue();
            CurrentSysteminfo.ramAvailableMB = RamAvailable.NextValue();
            CurrentSysteminfo.ramTotalMB = memKb / 1024;
            CurrentSysteminfo.diskUsedSpace = GameDrive.TotalSize - GameDrive.TotalFreeSpace;
            CurrentSysteminfo.diskFreeSpace = GameDrive.TotalFreeSpace;
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
                CurrentSysteminfo.totalPlayfieldserver      = ESGChildProcesses.Count();
                CurrentSysteminfo.totalPlayfieldserverRamMB = ESGChildProcesses.Aggregate(0L, (S, P) => S + P.PrivateMemorySize64);
            }

            SystemConfig.Current.ProcessInformation = ProcessInformation;
            SystemConfig.Save();
        }
        private void UpdateComputerInfos()
        {
            var CurrentAssembly = Assembly.GetAssembly(this.GetType());

            CurrentSysteminfo.serverName = EmpyrionConfiguration.DedicatedYaml.ServerName;
            CurrentSysteminfo.version = CurrentAssembly.GetAttribute<AssemblyFileVersionAttribute>()?.Version;
            CurrentSysteminfo.versionESG = EmpyrionConfiguration.Version;
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
                EGSRunState(true);
                Program.Host.ExposeShutdownHost();

                Process EGSProcess = Process.GetProcessById(ProcessInformation.Id);

                Request_ConsoleCommand(new PString("saveandexit " + aWaitMinutes));

                EGSProcess.WaitForExit((aWaitMinutes + 1) * 60000);
            }
            catch (Exception Error)
            {
                Logger.LogError(Error, "EGSStop");
                log(Error.ToString(), EmpyrionNetAPIDefinitions.LogLevel.Error);
            }
        }

        public void EGSStart()
        {
            try
            {
                Process EGSProcess = null;
                try { EGSProcess = Process.GetProcessById(ProcessInformation.Id); } catch { }

                if (EGSProcess != null && !EGSProcess.HasExited)
                {
                    EGSRunState(false);
                    return;
                }

                var StartCMD = string.IsNullOrEmpty(SystemConfig.Current.StartCMD) ? SystemConfig.Current.ProcessInformation.FileName : SystemConfig.Current.StartCMD;

                EGSProcess = new Process
                {
                    StartInfo = new ProcessStartInfo(Path.Combine(EmpyrionConfiguration.ProgramPath, Path.GetFileName(StartCMD)))
                    {
                        UseShellExecute  = !string.IsNullOrEmpty(SystemConfig.Current.StartCMD),
                        WindowStyle      = ProcessWindowStyle.Normal,
                        LoadUserProfile  = true,
                        CreateNoWindow   = false,
                        WorkingDirectory = EmpyrionConfiguration.ProgramPath,
                        Arguments        = SystemConfig.Current.ProcessInformation.Arguments,
                    }
                };

                EGSProcess.Start();
            }
            catch (Exception Error)
            {
                Logger.LogError(Error, "EGSStart");
                log(Error.ToString(), EmpyrionNetAPIDefinitions.LogLevel.Error);
            }
            EGSRunState(false);
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
        public IActionResult GetCurrentSysteminfo()
        {
            return Ok(SysteminfoManager.CurrentSysteminfo);
        }

        [HttpGet("StartCMDs")]
        public ActionResult<string[]> StartCMDs()
        {
            return Ok(Directory.EnumerateFiles(EmpyrionConfiguration.ProgramPath, "*.cmd").Select(F => Path.GetFileName(F)).ToArray());
        }

        [HttpGet("EGSStart")]
        public IActionResult EGSStart()
        {
            SysteminfoManager.EGSStart();
            return Ok();
        }

        [HttpGet("EGSStop/{aWaitMinutes}")]
        public IActionResult EGSStop(int aWaitMinutes)
        {
            SysteminfoManager.EGSStop(aWaitMinutes);
            return Ok();
        }

        
        [HttpGet("EGSRestart/{aWaitMinutes}")]
        public IActionResult EGSRestart(int aWaitMinutes)
        {
            SysteminfoManager.EGSStop(aWaitMinutes);
            SysteminfoManager.EGSStart();
            return Ok();
        }

        [HttpGet("SystemConfig")]
        public ActionResult<SystemConfig> GetCurrentSystemConfig()
        {
            return Ok(SysteminfoManager.SystemConfig.Current);
        }

        [HttpPost("SystemConfig")]
        public IActionResult SetCurrentSystemConfig([FromBody] SystemConfig aSystemConfig)
        {
            var SaveInfos = SysteminfoManager.SystemConfig.Current.ProcessInformation;
            SysteminfoManager.SystemConfig.Current = aSystemConfig;
            SysteminfoManager.SystemConfig.Current.ProcessInformation = SaveInfos;
            SysteminfoManager.SystemConfig.Save();
            return Ok();
        }

    }
}