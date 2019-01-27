using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionNetAPIAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading;

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

            TaskTools.Intervall(10000, () => GlobalStructureList());
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

        public class DeleteStructuresData
        {
            public int id { get; set; }
            public string playfield { get; set; }
        }

        [HttpPost("DeleteStructures")]
        public IActionResult DeleteStructures([FromBody]DeleteStructuresData[] aEntites)
        {
            aEntites
                .OrderBy(E => E.playfield)
                .ForEach(I =>
                {
                    try {
                        StructureManager.Request_Load_Playfield(new PlayfieldLoad(20, I.playfield, 0)).Wait();
                        Thread.Sleep(2000); // wait for Playfield finish
                    }
                    catch { }  // Playfield already loaded
                    StructureManager.Request_Entity_Destroy(new Id(I.id));
                });
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
