using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionModWebHost.Models;
using EmpyrionNetAPIAccess;
using EmpyrionNetAPITools;
using EWAExtenderCommunication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EmpyrionModWebHost.Controllers
{
    public enum RepeatEnum
    {
        manual,
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
        chatUntil,
        restart,
        startEGS,
        stopEGS,
        backupFull,
        backupStructure,
        backupSavegame,
        backupScenario,
        backupMods,
        backupEGSMainFiles,
        deleteOldBackups,
        deleteOldBackpacks,
        deletePlayerOnPlayfield,
        deleteHistoryBook,
        runShell,
        consoleCommand,
        wipePlayfield,
        resetPlayfield,
        recreatePlayfield,
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
        public Lazy<PlayfieldManager> PlayfieldManager { get; }
        public Lazy<SysteminfoManager> SysteminfoManager { get; }
        public Lazy<BackpackManager> BackpackManager { get; }
        public Lazy<HistoryBookManager> HistoryBookManager { get; }
        public ConfigurationManager<Timetable> TimetableConfig { get; private set; }

        public ILogger<TimetableManager> Logger { get; set; }


        public TimetableManager(ILogger<TimetableManager> aLogger)
        {
            Logger = aLogger;

            BackupManager       = new Lazy<BackupManager>       (() => Program.GetManager<BackupManager>());
            ChatManager         = new Lazy<ChatManager>         (() => Program.GetManager<ChatManager>());
            GameplayManager     = new Lazy<GameplayManager>     (() => Program.GetManager<GameplayManager>());
            PlayerManager       = new Lazy<PlayerManager>       (() => Program.GetManager<PlayerManager>());
            PlayfieldManager    = new Lazy<PlayfieldManager>    (() => Program.GetManager<PlayfieldManager>());
            SysteminfoManager   = new Lazy<SysteminfoManager>   (() => Program.GetManager<SysteminfoManager>());
            BackpackManager     = new Lazy<BackpackManager>     (() => Program.GetManager<BackpackManager>());
            HistoryBookManager  = new Lazy<HistoryBookManager>  (() => Program.GetManager<HistoryBookManager>());

            TimetableConfig = new ConfigurationManager<Timetable>
            {
                ConfigFilename = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "EWA", "DB", "Timetable.xml")
            };
            TimetableConfig.Load();
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            LogLevel = EmpyrionNetAPIDefinitions.LogLevel.Debug;

            TaskTools.Intervall(60, CheckTimetable);
        }

        private void CheckTimetable()
        {
            if (TimetableConfig.Current.Actions == null) return;

            TimetableConfig.Current.Actions
                .Where(A => A.active)
                .Where(A => IsReverseTime(A) ? A.timestamp >= DateTime.Now && A.nextExecute <= DateTime.Now : A.nextExecute <= DateTime.Now)
                .ToArray()
                .ForEach(A => {
                    A.nextExecute = GetNextExecute(A, IsReverseTime(A) ? ReverseAutoRepeatTime(A.repeat) : A.repeat, false);
                    TimetableConfig.Save();
                    RunThis(A);
                });

            TimetableConfig.Current.Actions
                .Where(A => A.active && IsReverseTime(A) && A.timestamp <= DateTime.Now)
                .ToArray()
                .ForEach(A => A.active = false);
        }

        private RepeatEnum ReverseAutoRepeatTime(RepeatEnum aRepeat)
        {
            switch (aRepeat)
            {
                case RepeatEnum.manual:
                case RepeatEnum.min5:
                case RepeatEnum.min10:
                case RepeatEnum.min15:
                case RepeatEnum.min20:
                case RepeatEnum.min30:
                case RepeatEnum.min45:
                case RepeatEnum.hour1:
                case RepeatEnum.hour2:
                case RepeatEnum.hour3:
                case RepeatEnum.hour6:
                case RepeatEnum.hour12: return aRepeat;
                case RepeatEnum.day1:
                case RepeatEnum.dailyAt:
                case RepeatEnum.mondayAt:
                case RepeatEnum.tuesdayAt:
                case RepeatEnum.wednesdayAt:
                case RepeatEnum.thursdayAt:
                case RepeatEnum.fridayAt:
                case RepeatEnum.saturdayAt:
                case RepeatEnum.sundayAt:
                case RepeatEnum.monthly: return RepeatEnum.hour1;
                case RepeatEnum.timeAt:
                default: return RepeatEnum.hour1;
            }
        }

        private bool IsReverseTime(TimetableAction aAction)
        {
            return aAction.actionType == ActionType.chatUntil;
        }

        public void InitTimetableNextExecute(TimetableAction[] aActions)
        {
            if (aActions == null) return;

            aActions
                .Where(A => A.active)
                .ToArray()
                .ForEach(A => {
                    A.nextExecute = GetNextExecute(A, IsReverseTime(A) ? ReverseAutoRepeatTime(A.repeat) : A.repeat, true);
                    if (IsReverseTime(A) && A.timestamp.Year == 1) A.timestamp = GetNextExecute(A, A.repeat, true).Date + A.timestamp.TimeOfDay;
                });
        }

        private DateTime GetNextExecute(TimetableAction aAction, RepeatEnum aRepeat, bool initMode)
        {
            switch (aRepeat)
            {
                case RepeatEnum.manual      : return DateTime.MaxValue;
                case RepeatEnum.min5        : return DateTime.Now   + (initMode ? aAction.timestamp.TimeOfDay : new TimeSpan(0,  5, 0));
                case RepeatEnum.min10       : return DateTime.Now   + (initMode ? aAction.timestamp.TimeOfDay : new TimeSpan(0, 10, 0));
                case RepeatEnum.min15       : return DateTime.Now   + (initMode ? aAction.timestamp.TimeOfDay : new TimeSpan(0, 15, 0));
                case RepeatEnum.min20       : return DateTime.Now   + (initMode ? aAction.timestamp.TimeOfDay : new TimeSpan(0, 20, 0));
                case RepeatEnum.min30       : return DateTime.Now   + (initMode ? aAction.timestamp.TimeOfDay : new TimeSpan(0, 30, 0));
                case RepeatEnum.min45       : return DateTime.Now   + (initMode ? aAction.timestamp.TimeOfDay : new TimeSpan(0, 45, 0));
                case RepeatEnum.hour1       : return DateTime.Now   + (initMode ? aAction.timestamp.TimeOfDay : new TimeSpan(1,  0, 0));
                case RepeatEnum.hour2       : return DateTime.Now   + (initMode ? aAction.timestamp.TimeOfDay : new TimeSpan(2,  0, 0));
                case RepeatEnum.hour3       : return DateTime.Now   + (initMode ? aAction.timestamp.TimeOfDay : new TimeSpan(3,  0, 0));
                case RepeatEnum.hour6       : return DateTime.Now   + (initMode ? aAction.timestamp.TimeOfDay : new TimeSpan(6,  0, 0));
                case RepeatEnum.hour12      : return DateTime.Now   + (initMode ? aAction.timestamp.TimeOfDay : new TimeSpan(12, 0, 0));
                case RepeatEnum.day1        : return DateTime.Now   + (initMode ? aAction.timestamp.TimeOfDay : new TimeSpan(24, 0, 0));
                case RepeatEnum.dailyAt     : return DateTime.Today + new TimeSpan(aAction.timestamp.TimeOfDay > DateTime.Now.TimeOfDay ? 0 : 24, 0, 0) + aAction.timestamp.TimeOfDay;
                case RepeatEnum.mondayAt    : return GetNextWeekday(DateTime.Today, DayOfWeek.Monday)    + aAction.timestamp.TimeOfDay;
                case RepeatEnum.tuesdayAt   : return GetNextWeekday(DateTime.Today, DayOfWeek.Tuesday)   + aAction.timestamp.TimeOfDay;
                case RepeatEnum.wednesdayAt : return GetNextWeekday(DateTime.Today, DayOfWeek.Wednesday) + aAction.timestamp.TimeOfDay;
                case RepeatEnum.thursdayAt  : return GetNextWeekday(DateTime.Today, DayOfWeek.Thursday)  + aAction.timestamp.TimeOfDay;
                case RepeatEnum.fridayAt    : return GetNextWeekday(DateTime.Today, DayOfWeek.Friday)    + aAction.timestamp.TimeOfDay;
                case RepeatEnum.saturdayAt  : return GetNextWeekday(DateTime.Today, DayOfWeek.Saturday)  + aAction.timestamp.TimeOfDay;
                case RepeatEnum.sundayAt    : return GetNextWeekday(DateTime.Today, DayOfWeek.Sunday)    + aAction.timestamp.TimeOfDay;
                case RepeatEnum.monthly     : return new DateTime(DateTime.Today.Year, (DateTime.Today.Month % 12) + 1, DateTime.Today.Day) + new TimeSpan(24, 0, 0) + aAction.timestamp.TimeOfDay;
                case RepeatEnum.timeAt      : return (aAction.timestamp.TimeOfDay > DateTime.Now.TimeOfDay ? DateTime.Today : DateTime.MaxValue.Date) + aAction.timestamp.TimeOfDay;
                default:                      return DateTime.MaxValue;
            }
        }

        public static DateTime GetNextWeekday(DateTime start, DayOfWeek day)
        {
            // The (... + 7) % 7 ensures we end up with a value in the range [0, 6]
            int daysToAdd = ((int)day - (int)start.DayOfWeek + 7) % 7;
            return start.AddDays(daysToAdd == 0 ? 7 : daysToAdd);
        }

        public void RunThis(SubTimetableAction aAction)
        {
            switch (aAction.actionType)
            {
                case ActionType.chat                    : ChatManager.Value.ChatMessageSERV(aAction.data); break;
                case ActionType.chatUntil               : ChatManager.Value.ChatMessageSERV(aAction.data); break;
                case ActionType.restart                 : EGSRestart(aAction); break;
                case ActionType.startEGS                : SysteminfoManager.Value.EGSStart(); break;
                case ActionType.stopEGS                 : SysteminfoManager.Value.EGSStop(int.TryParse(aAction.data, out int WaitMinutes) ? WaitMinutes : 0); ; break;
                case ActionType.backupFull              : BackupManager.Value.FullBackup        (BackupManager.Value.CurrentBackupDirectory("")); break;
                case ActionType.backupStructure         : BackupManager.Value.StructureBackup   (BackupManager.Value.CurrentBackupDirectory(" - Structures")); break;
                case ActionType.backupSavegame          : BackupManager.Value.SavegameBackup    (BackupManager.Value.CurrentBackupDirectory(" - Savegame")); break;
                case ActionType.backupScenario          : BackupManager.Value.ScenarioBackup    (BackupManager.Value.CurrentBackupDirectory(" - Scenario")); break;
                case ActionType.backupMods              : BackupManager.Value.ModsBackup        (BackupManager.Value.CurrentBackupDirectory(" - Mods")); break;
                case ActionType.backupEGSMainFiles      : BackupManager.Value.EGSMainFilesBackup(BackupManager.Value.CurrentBackupDirectory(" - ESG MainFiles")); break;
                case ActionType.deleteOldBackups        : BackupManager.Value.DeleteOldBackups  (int.TryParse(aAction.data, out int BackupDays) ? BackupDays : 14); break;
                case ActionType.deleteOldBackpacks      : BackpackManager.Value.DeleteOldBackpacks(int.TryParse(aAction.data, out int BackpackDays) ? BackpackDays : 14); break;
                case ActionType.deletePlayerOnPlayfield : DeletePlayerOnPlayfield(aAction); break;
                case ActionType.deleteHistoryBook       : HistoryBookManager.Value.DeleteHistory(int.TryParse(aAction.data, out int HistoryDays) ? HistoryDays : 14); break;
                case ActionType.runShell                : ExecShell(aAction); break;
                case ActionType.consoleCommand          : GameplayManager.Value.Request_ConsoleCommand(new PString(aAction.data)); break;
                case ActionType.wipePlayfield           : PlayfieldManager.Value.Wipe(aAction.data.Split(':')[1].Split(';').Select(P => P.Trim()), aAction.data.Split(':')[0]); break;
                case ActionType.resetPlayfield          : PlayfieldManager.Value.ResetPlayfield(aAction.data.Split(';').Select(P => P.Trim()).ToArray()); break;
                case ActionType.recreatePlayfield       : PlayfieldManager.Value.RecreatePlayfield(aAction.data.Split(';').Select(P => P.Trim()).ToArray()); break;
            }

            if (aAction.actionType != ActionType.restart) ExecSubActions(aAction);
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


        private void EGSRestart(SubTimetableAction aAction)
        {
            RestartState(true);
            try
            {
                SysteminfoManager.Value.EGSStop(int.TryParse(aAction.data, out int WaitMinutes) ? WaitMinutes : 0);

                Thread.Sleep(10000);
                ExecSubActions(aAction);

                SysteminfoManager.Value.EGSStart();
            }
            catch (Exception Error)
            {
                Logger.LogError(Error, "EGSRestart");
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
                MainAction.subAction.ForEach(async A => await Program.Host.SaveApiCall(() => RunThis(A), this, $"{A.actionType}"));
            }
        }
    }

    [ApiController]
    [Authorize(Roles = nameof(Role.InGameAdmin))]
    [Route("[controller]")]
    public class TimetableController : ControllerBase
    {

        public TimetableManager TimetableManager { get; }
        public ILogger<TimetableController> Logger { get; set; }

        public TimetableController(ILogger<TimetableController> aLogger)
        {
            Logger = aLogger;
            TimetableManager = Program.GetManager<TimetableManager>();
        }

        [HttpGet("GetTimetable")]
        public ActionResult<TimetableAction[]> GetTimetable()
        {
            return TimetableManager.TimetableConfig.Current.Actions;
        }

        [HttpPost("SetTimetable")]
        public IActionResult SetTimetable([FromBody]TimetableAction[] aActions)
        {
            TimetableManager.InitTimetableNextExecute(aActions);
            TimetableManager.TimetableConfig.Current.Actions = aActions;
            TimetableManager.TimetableConfig.Save();
            return Ok();
        }

        [HttpPost("RunThis")]
        public async Task<IActionResult> RunThis([FromBody]SubTimetableAction aAction)
        {
            await Program.Host.SaveApiCall(() => TimetableManager.RunThis(aAction), TimetableManager, $"{aAction.actionType}");
            return Ok();
        }

    }
}