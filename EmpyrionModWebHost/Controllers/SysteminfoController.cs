using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionNetAPIAccess;
using EWAExtenderCommunication;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace EmpyrionModWebHost.Controllers
{
    public class SysteminfoDataModel
    {
        public bool online;
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


    [Authorize]
    public class SysteminfoHub : Hub
    {
        private SysteminfoManager SysteminfoManager { get; set; }
    }

    public class SysteminfoManager : EmpyrionModBase, IEWAPlugin, IClientHostCommunication
    {
        private SysteminfoDataModel CurrentSysteminfo = new SysteminfoDataModel();

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

        public SysteminfoManager(IHubContext<SysteminfoHub> aSysteminfoHub)
        {
            SysteminfoHub = aSysteminfoHub;
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

            TaskWait.Intervall(2000,  () => SysteminfoHub?.Clients.All.SendAsync("UPC", JsonConvert.SerializeObject(new {
                o       = CurrentSysteminfo.online,
                ap      = CurrentSysteminfo.activePlayers,
                apf     = CurrentSysteminfo.activePlayfields,
                c       = CurrentSysteminfo.cpuTotalLoad,
                r       = CurrentSysteminfo.ramAvailableMB,
                tpf     = CurrentSysteminfo.totalPlayfieldserver,
                tpfm    = CurrentSysteminfo.totalPlayfieldserverRamMB,
            })).Wait(1000));
            TaskWait.Intervall(30000, () => SysteminfoHub?.Clients.All.SendAsync("Update", JsonConvert.SerializeObject(CurrentSysteminfo)).Wait(1000));
            TaskWait.Intervall(5000, UpdateEmpyrionInfosAsync);
            TaskWait.Intervall(5000, UpdateComputerInfos);
            TaskWait.Intervall(2000, UpdatePerformanceInfos);

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
            var GameDrive = DriveInfo.GetDrives().FirstOrDefault(D => D.RootDirectory.FullName == Path.GetPathRoot(ProcessInformation == null ? Directory.GetCurrentDirectory() : ProcessInformation.CurrentDirecrory));

            GetPhysicallyInstalledSystemMemory(out long memKb);

            CurrentSysteminfo.cpuTotalLoad = CpuTotalLoad.NextValue();
            CurrentSysteminfo.ramAvailableMB = RamAvailable.NextValue();
            CurrentSysteminfo.ramTotalMB = memKb / 1024;
            CurrentSysteminfo.diskUsedSpace = GameDrive.TotalSize - GameDrive.TotalFreeSpace;
            CurrentSysteminfo.diskFreeSpace = GameDrive.TotalFreeSpace;
        }

        private void UpdateEmpyrionInfosAsync()
        {
            CurrentSysteminfo.online = (DateTime.Now - LastProcessInformationUpdate).TotalSeconds <= 10;

            if (ToEmpyrion == null) return;

            ToEmpyrion.SendMessage(new ClientHostComData() { Command = ClientHostCommand.ProcessInformation });
            if (ProcessInformation == null) return;

            CurrentSysteminfo.activePlayers    = PlayerManager.OnlinePlayersCount;
            var activePlayfields               = TaskWait.For(2, Request_Playfield_List()).Result.playfields;
            CurrentSysteminfo.activePlayfields = activePlayfields == null ? 0 : activePlayfields.Count;

            var ESGProcess        = Process.GetProcessById(ProcessInformation.Id);
            var ESGChildProcesses = ESGProcess?.GetChildProcesses().Where(P => P.ProcessName == "EmpyrionPlayfieldServer").ToArray();

            if (ESGChildProcesses != null)
            {
                CurrentSysteminfo.totalPlayfieldserver      = ESGChildProcesses.Count();
                CurrentSysteminfo.totalPlayfieldserverRamMB = ESGChildProcesses.Aggregate(0L, (S, P) => S + P.PrivateMemorySize64);
            }
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
                    ProcessInformation = aMessage.Data as ProcessInformation; break;
            }
        }

    }

    [Authorize]
    public class SysteminfoController : ODataController
    {
        public IHubContext<SysteminfoHub> SysteminfoHub { get; }
        public SysteminfoManager SysteminfoManager { get; }

        public SysteminfoController(IHubContext<SysteminfoHub> aSysteminfoHub)
        {
            SysteminfoHub = aSysteminfoHub;
            SysteminfoManager = Program.GetManager<SysteminfoManager>();
            SysteminfoManager.SysteminfoHub = aSysteminfoHub;
        }

    }
}
