using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionModWebHost.Models;
using EmpyrionNetAPIAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EmpyrionModWebHost.Controllers
{

    public class GlobalStructureListBackup
    {
        public Tuple<string, GlobalStructureInfo[]>[] Structures { get; set; }
    }

    public class StructureManager : EmpyrionModBase, IEWAPlugin
    {

        public ModGameAPI GameAPI { get; private set; }
        public ConfigurationManager<GlobalStructureList> LastGlobalStructureList { get; private set; }

        public StructureManager()
        {
            LastGlobalStructureList = new ConfigurationManager<GlobalStructureList>()
            {
                UseJSON        = true,
                ConfigFilename = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "DB", "GlobalStructureList.json")
            };
            LastGlobalStructureList.Load();
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
        }

        public GlobalStructureList GlobalStructureList()
        {
            try
            {
                LastGlobalStructureList.Current = Request_GlobalStructure_List().Result;
                TaskTools.Delay(0, () => LastGlobalStructureList.Save());
            }
            catch { }

            return LastGlobalStructureList.Current;
        }

    }

    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class StructureController : ControllerBase
    {
        public StructureManager StructureManager { get; }

        public StructureController()
        {
            StructureManager = Program.GetManager<StructureManager>();
        }

        [HttpGet("GlobalStructureList")]
        public IActionResult GlobalStructureList()
        {
            return Ok(StructureManager.GlobalStructureList());
        }

        [HttpPost("DeleteStructures")]
        public IActionResult DeleteStructures([FromBody]int[] aEntityIds)
        {
            aEntityIds.ForEach(I => StructureManager.Request_Entity_Destroy(new Id(I)));
            return Ok();
        }

        public class SetFactionOfStucturesData
        {
            public string FactionAbbrev { get; set; }
            public int[] EntityIds { get; set; }
        }

        [HttpPost("SetFactionOfStuctures")]
        public IActionResult SetFactionOfStuctures([FromBody]SetFactionOfStucturesData aData)
        {
            aData.EntityIds.ForEach(I => StructureManager.Request_ConsoleCommand(new PString($"faction entity '{aData.FactionAbbrev}' {I}")));
            return Ok();
        }

    }
}
