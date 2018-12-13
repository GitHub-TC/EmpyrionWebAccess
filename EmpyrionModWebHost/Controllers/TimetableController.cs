using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionNetAPIAccess;
using EWAExtenderCommunication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmpyrionModWebHost.Controllers
{
    public enum RepeatEnum
    {
        min5,
        min10,
        min15,
        min20,
        min30,
        min45,
        hour1,
        hour2,
        hour3,
        hour6,
        hour12,
        day1,
        dailyAt,
        mondayAt,
        tuesdayAt,
        wednesdayAt,
        thursdayAt,
        fridayAt,
        saturdayAt,
        sundayAt,
        monthly,
        timeAt
    }

    public enum ActionType
    {
        chat,
        restart,
        startEGS,
        stopEGS,
        backupFull,
        backupStructure,
        backupSavegame,
        backupScenario,
        backupMods,
        backupEGSMainFiles,
        deletePlayerOnPlayfield,
        runShell,
        consoleCommand,
    }

    public class TimetableAction : SubTimetableAction
    {
        public DateTime timestamp { get; set; }
        public RepeatEnum repeat { get; set; }
        public SubTimetableAction[] subAction { get; set; }
        public DateTime nextExecute { get; set; }
    }

    public class SubTimetableAction
    {
        public bool active { get; set; }
        public ActionType actionType { get; set; }
        public string data { get; set; }
    }


    public class Timetable
    {
        public TimetableAction[] Actions { get; set; }
    }

    public class TimetableManager : EmpyrionModBase, IEWAPlugin
    {
        public ModGameAPI GameAPI { get; private set; }
        public Lazy<BackupManager> BackupManager { get; }
        public Lazy<ChatManager> ChatManager { get; }
        public Lazy<GameplayManager> GameplayManager { get; }
        public Lazy<PlayerManager> PlayerManager { get; }
        public Lazy<SysteminfoManager> SysteminfoManager { get; }
        public ConfigurationManager<Timetable> TimetableConfig { get; private set; }

        public TimetableManager()
        {
            BackupManager       = new Lazy<BackupManager>       (() => Program.GetManager<BackupManager>());
            ChatManager         = new Lazy<ChatManager>         (() => Program.GetManager<ChatManager>());
            GameplayManager     = new Lazy<GameplayManager>     (() => Program.GetManager<GameplayManager>());
            PlayerManager       = new Lazy<PlayerManager>       (() => Program.GetManager<PlayerManager>());
            SysteminfoManager   = new Lazy<SysteminfoManager>   (() => Program.GetManager<SysteminfoManager>());

            TimetableConfig = new ConfigurationManager<Timetable>
            {
                ConfigFilename = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "DB", "Timetable.xml")
            };
            TimetableConfig.Load();
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            LogLevel = EmpyrionNetAPIDefinitions.LogLevel.Debug;
        }

        public void RunThis(SubTimetableAction aAction)
        {
            switch (aAction.actionType)
            {
                case ActionType.chat                    : ChatManager.Value.ChatMessage(null, null, null, aAction.data); break;
                case ActionType.restart                 : EGSRestart(aAction); break;
                case ActionType.startEGS                : EGSStart(aAction); break;
                case ActionType.stopEGS                 : EGSStop(aAction); break;
                case ActionType.backupFull              : BackupManager.Value.FullBackup(BackupManager.Value.CurrentBackupDirectory); break;
                case ActionType.backupStructure         : BackupManager.Value.StructureBackup(BackupManager.Value.CurrentBackupDirectory); break;
                case ActionType.backupSavegame          : BackupManager.Value.SavegameBackup(BackupManager.Value.CurrentBackupDirectory); break;
                case ActionType.backupScenario          : BackupManager.Value.ScenarioBackup(BackupManager.Value.CurrentBackupDirectory); break;
                case ActionType.backupMods              : BackupManager.Value.ModsBackup(BackupManager.Value.CurrentBackupDirectory); break;
                case ActionType.backupEGSMainFiles      : BackupManager.Value.EGSMainFilesBackup(BackupManager.Value.CurrentBackupDirectory); break;
                case ActionType.deletePlayerOnPlayfield : DeletePlayerOnPlayfield(aAction); break;
                case ActionType.runShell                : ExecShell(aAction); break;
                case ActionType.consoleCommand          : GameplayManager.Value.Request_ConsoleCommand(new PString(aAction.data)); break;
            }

            if(aAction.actionType != ActionType.restart) ExecSubActions(aAction);
        }

        private void DeletePlayerOnPlayfield(SubTimetableAction aAction)
        {
            var OnPlayfields = aAction.data.Split(";").Select(P => P.Trim());
            PlayerManager.Value
                .QueryPlayer(DB => DB.Players.Where(P => OnPlayfields.Contains(P.Playfield)), 
                P => GameplayManager.Value.WipePlayer(P.SteamId));
        }

        public void RestartState(bool aRunning)
        {
            SysteminfoManager.Value.CurrentSysteminfo.online =
                SysteminfoManager.Value.SetState(SysteminfoManager.Value.CurrentSysteminfo.online, "r", aRunning);
        }

        public void EGSRunState(bool aRunning)
        {
            SysteminfoManager.Value.CurrentSysteminfo.online =
                SysteminfoManager.Value.SetState(SysteminfoManager.Value.CurrentSysteminfo.online, "S", aRunning);
        }

        private void EGSStop(SubTimetableAction aAction)
        {
            try
            {
                EGSRunState(true);
                Program.Host.ExposeShutdownHost();

                Process EGSProcess = null;
                try { EGSProcess = Process.GetProcessById(SysteminfoManager.Value.ProcessInformation.Id); } catch { }

                GameplayManager.Value.Request_ConsoleCommand(new PString("saveandexit 0"));

                EGSProcess.WaitForExit(120000);
            }
            catch (Exception Error)
            {
                log(Error.ToString(), EmpyrionNetAPIDefinitions.LogLevel.Error);
            }
        }

        private void EGSStart(SubTimetableAction aAction)
        {
            var EGSProcess = new Process
            {
                StartInfo = new ProcessStartInfo(SysteminfoManager.Value.SystemConfig.Current.ProcessInformation.FileName)
                {
                    UseShellExecute     = false,
                    CreateNoWindow      = true,
                    WorkingDirectory    = EmpyrionConfiguration.ProgramPath,
                    Arguments           = SysteminfoManager.Value.SystemConfig.Current.ProcessInformation.Arguments,
                }
            };

            EGSProcess.Start();
            EGSRunState(false);
        }

        private void EGSRestart(SubTimetableAction aAction)
        {
            RestartState(true);
            try
            {
                EGSStop(aAction);

                Thread.Sleep(30000);

                ExecSubActions(aAction);

                EGSStart(aAction);
            }
            catch (Exception Error)
            {
                log(Error.ToString(), EmpyrionNetAPIDefinitions.LogLevel.Error);
            }

            RestartState(false);
        }

        private void ExecShell(SubTimetableAction aAction)
        {
            var ExecProcess = new Process
            {
                StartInfo = new ProcessStartInfo(aAction.data)
                {
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WorkingDirectory = EmpyrionConfiguration.SaveGamePath,
                }
            };

            ExecProcess.Start();
            ExecProcess.WaitForExit(60000);
        }

        private void ExecSubActions(SubTimetableAction aAction)
        {
            if (aAction is TimetableAction MainAction && MainAction.subAction != null)
            {
                MainAction.subAction.ForEach(A => Program.Host.SaveApiCall(() => RunThis(A), this, $"{A.actionType}"));
            }
        }
    }

    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class TimetableController : ControllerBase
    {

        public TimetableManager TimetableManager { get; }

        public TimetableController()
        {
            TimetableManager = Program.GetManager<TimetableManager>();
        }

        [HttpGet("GetTimetable")]
        public ActionResult<TimetableAction[]> GetTimetable()
        {
            return TimetableManager.TimetableConfig.Current.Actions;
        }

        [HttpPost("SetTimetable")]
        public IActionResult ReadStructures([FromBody]TimetableAction[] aActions)
        {
            TimetableManager.TimetableConfig.Current.Actions = aActions;
            TimetableManager.TimetableConfig.Save();
            return Ok();
        }

        [HttpPost("RunThis")]
        public IActionResult RunThis([FromBody]SubTimetableAction aAction)
        {
            Program.Host.SaveApiCall(() => TimetableManager.RunThis(aAction), TimetableManager, $"{aAction.actionType}");
            return Ok();
        }

    }
}