using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionNetAPIAccess;
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
        backupFull,
        backupStructure,
        deletePlayerOnPlayfield,
        runShell,
        consoleCommand
    }

    public class TimetableAction
    {
        public bool active { get; set; }
        public ActionType actionType { get; set; }
        public string data { get; set; }
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
        public ConfigurationManager<Timetable> TimetableConfig { get; private set; }

        public TimetableManager()
        {
            BackupManager   = new Lazy<BackupManager>   (() => Program.GetManager<BackupManager>());
            ChatManager     = new Lazy<ChatManager>     (() => Program.GetManager<ChatManager>());
            GameplayManager = new Lazy<GameplayManager> (() => Program.GetManager<GameplayManager>());

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

    }
}