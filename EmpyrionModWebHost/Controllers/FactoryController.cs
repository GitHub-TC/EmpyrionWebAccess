using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionNetAPIAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EmpyrionModWebHost.Controllers
{

    public class FactoryManager : EmpyrionModBase, IEWAPlugin
    {

        public ModGameAPI GameAPI { get; private set; }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
        }

    }

    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class FactoryController : ControllerBase
    {
        public FactoryManager FactoryManager { get; }

        public FactoryController()
        {
            FactoryManager = Program.GetManager<FactoryManager>();
        }

        [HttpGet("GetBlueprintResources/{aPlayerId}")]
        public async System.Threading.Tasks.Task<ActionResult<BlueprintResources>> GetBlueprintResourcesAsync(int aPlayerId)
        {
            try
            {
                var PlayerInfo = await FactoryManager.Request_Player_Info(new Id(aPlayerId));

                var Result = new BlueprintResources()
                {
                    PlayerId = aPlayerId,
                    ItemStacks = new List<ItemStack>(),
                    ReplaceExisting = true,
                };
                PlayerInfo.bpResourcesInFactory?.ForEach(I => Result.ItemStacks.Add(new ItemStack() { id = I.Key, count = (int)I.Value }));
                return Ok(Result);
            }
            catch (Exception Error)
            {
                return NotFound(Error.Message);
            }
        }

        [HttpPost("SetBlueprintResources")]
        public IActionResult SetBlueprintResources([FromBody]BlueprintResources aRessources)
        {
            try
            {
                FactoryManager.Request_Blueprint_Resources(aRessources).Wait(1000);
                return Ok();
            }
            catch (Exception Error)
            {
                return NotFound(Error.Message);
            }
        }

        [HttpGet("FinishBlueprint/{aPlayerId}")]
        public IActionResult FinishBlueprint(int aPlayerId)
        {
            try
            {
                FactoryManager.Request_Blueprint_Finish(new Id(aPlayerId)).Wait(1000);
                return Ok();
            }
            catch (Exception Error)
            {
                return NotFound(Error.Message);
            }
        }
    }
}