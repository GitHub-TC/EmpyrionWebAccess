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

    public class StructureManager : EmpyrionModBase, IEWAPlugin
    {

        public ModGameAPI GameAPI { get; private set; }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
        }

        public GlobalStructureList GlobalStructureListAsync()
        {
            return Request_GlobalStructure_List().Result;
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
            return Ok(StructureManager.GlobalStructureListAsync());
        }

        [HttpPost("DeleteStructures")]
        public IActionResult DeleteStructures([FromBody]int[] aEntityIds)
        {
            aEntityIds.ForEach(I => StructureManager.Request_Entity_Destroy(new Id(I)));
            return Ok();
        }

        [HttpPost("SetToAdmin")]
        public IActionResult SetToAdmin([FromBody]int[] aEntityIds)
        {
            aEntityIds.ForEach(I => StructureManager.Request_ConsoleCommand(new PString($"faction entity 'Adm' {I}")));
            return Ok();
        }

        [HttpPost("SetToAlien")]
        public IActionResult SetToAlien([FromBody]int[] aEntityIds)
        {
            aEntityIds.ForEach(I => StructureManager.Request_ConsoleCommand(new PString($"faction entity 'Aln' {I}")));
            return Ok();
        }

        [HttpPost("SetFactionOfStuctures/{aFactionAbbrev}")]
        public IActionResult SetFactionOfStuctures(string aFactionAbbrev, [FromBody]int[] aEntityIds)
        {
            aEntityIds.ForEach(I => StructureManager.Request_ConsoleCommand(new PString($"faction entity '{aFactionAbbrev}' {I}")));
            return Ok();
        }

    }
}
