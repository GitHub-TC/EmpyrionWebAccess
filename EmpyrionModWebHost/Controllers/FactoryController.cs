using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionModWebHost.Models;
using EmpyrionNetAPIAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

    [ApiController]
    [Authorize(Roles = nameof(Role.Moderator))]
    [Route("[controller]")]
    public class FactoryController : ControllerBase
    {
        public FactoryManager FactoryManager { get; }

        public FactoryController()
        {
            FactoryManager = Program.GetManager<FactoryManager>();
        }

        [HttpGet("GetBlueprintResources/{aPlayerId}")]
        public async Task<ActionResult<BlueprintResources>> GetBlueprintResources(int aPlayerId)
        {
            try
            {
                var onlinePlayers = await FactoryManager.Request_Player_List();
                if(!onlinePlayers.list.Contains(aPlayerId)) return NotFound($"Player {aPlayerId} not online");

                var PlayerInfo = await FactoryManager.Request_Player_Info(aPlayerId.ToId());

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
        [Authorize(Roles = nameof(Role.GameMaster))]
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