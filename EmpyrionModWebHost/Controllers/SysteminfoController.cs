using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionNetAPIAccess;
using EWAExtenderCommunication;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Mvc;
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
        public string version;
        public int activePlayers;
        public int activePlayfields;
        public int totalPlayfieldserver;
        public long diskFreeSpace;
        public long diskUsedSpace;
        public float cpuTotalLoad;
        public float ramAvailableMB;
        public long ramTotalMB;
        public string serverName;
    }


    public class SysteminfoHub : Hub
    {
        private SysteminfoManager SysteminfoManager { get; set; }
    }

    public class SysteminfoManager : EmpyrionModBase, IEWAPlugin, IClientHostCommunication
    {
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
            if (ToEmpyrion == null) return;

            var GameDrive = DriveInfo.GetDrives().FirstOrDefault(D => D.RootDirectory.FullName == Path.GetPathRoot(ProcessInformation == null ? Directory.GetCurrentDirectory() : ProcessInformation.CurrentDirecrory));

            var CurrentAssembly = Assembly.GetAssembly(this.GetType());
            GetPhysicallyInstalledSystemMemory(out long memKb);

            var Systeminfo = new SysteminfoDataModel()
            {
                online = (DateTime.Now - LastProcessInformationUpdate).TotalSeconds <= 10,
                serverName = "abc",
                version = $"{CurrentAssembly.GetAttribute<AssemblyTitleAttribute>()?.Title} by {CurrentAssembly.GetAttribute<AssemblyCompanyAttribute>()?.Company} Version:{CurrentAssembly.GetAttribute<AssemblyFileVersionAttribute>()?.Version}",
                cpuTotalLoad = CpuTotalLoad.NextValue(),
                ramAvailableMB = RamAvailable.NextValue(),
                ramTotalMB = memKb / 1024,
                diskUsedSpace = GameDrive.TotalSize - GameDrive.TotalFreeSpace,
                diskFreeSpace = GameDrive.TotalFreeSpace,
                activePlayers = PlayerManager.OnlinePlayersCount,
                activePlayfields = 0,
                totalPlayfieldserver = 0,
            };
            SysteminfoHub?.Clients.All.SendAsync("Update", JsonConvert.SerializeObject(Systeminfo)).Wait();
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            LogLevel = EmpyrionNetAPIDefinitions.LogLevel.Debug;

            PlayerManager = Program.GetManager<PlayerManager>();

            new Thread(() => {
                while (true)
                {
                    ToEmpyrion.SendMessage(new ClientHostComData() { Command = ClientHostCommand.ProcessInformation });
                    Thread.Sleep(5000);
                    UpdateSystemInfo();
                }
            }).Start();

            CpuTotalLoad = new PerformanceCounter
            {
                CategoryName = "Processor",
                CounterName = "% Processor Time",
                InstanceName = "_Total"
            };

            RamAvailable = new PerformanceCounter("Memory", "Available MBytes");
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
