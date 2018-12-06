using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionModWebHost.Models;
using EmpyrionNetAPIAccess;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OData.Edm;
using Newtonsoft.Json;

namespace EmpyrionModWebHost.Controllers
{
    [Authorize]
    public class BackpackHub : Hub
    {
        private BackpackManager BackpackManager { get; set; }
    }

    public class BackpackManager : EmpyrionModBase, IEWAPlugin, IDatabaseConnect
    {
        public ModGameAPI GameAPI { get; private set; }

        public BackpackManager(IHubContext<BackpackHub> aBackpackHub)
        {
            BackpackHub = aBackpackHub;
        }

        public void CreateAndUpdateDatabase()
        {
            using (var DB = new BackpackContext())
            {
                DB.Database.EnsureCreated();
            }
        }

        public void UpdateBackpack(string aPlayerSteamId, ItemStack[] aToolbar, ItemStack[] aBag)
        {
            using (var DB = new BackpackContext())
            {
                var Backpack = DB.Backpacks
                    .OrderByDescending(B => B.Timestamp)
                    .FirstOrDefault(B => B.Id == aPlayerSteamId);
                var IsNewBackpack = Backpack == null || (DateTime.Now - Backpack.Timestamp).TotalMinutes >= 1;
                var ToolbarContent = JsonConvert.SerializeObject(aToolbar);
                var BagContent     = JsonConvert.SerializeObject(aBag);

                if (Backpack?.ToolbarContent == ToolbarContent && Backpack?.BagContent == BagContent) return;

                if (IsNewBackpack)
                {
                    Backpack = new Backpack()
                    {
                        Id              = aPlayerSteamId,
                        Timestamp       = DateTime.Now,
                        ToolbarContent  = ToolbarContent,
                        BagContent      = BagContent
                    };
                    DB.Backpacks.Add(Backpack);
                }

                DB.SaveChanges();

                Backpack = DB.Backpacks.FirstOrDefault(B => B.Id == aPlayerSteamId && B.Timestamp == DateTime.MinValue);
                IsNewBackpack = Backpack == null;
                if(IsNewBackpack) Backpack = new Backpack() {
                                                                Id = aPlayerSteamId,
                                                                Timestamp = DateTime.MinValue,
                                                            };

                Backpack.ToolbarContent = ToolbarContent;
                Backpack.BagContent     = BagContent;
                if (IsNewBackpack) DB.Backpacks.Add(Backpack);

                var count = DB.SaveChanges();

                if(count > 0) BackpackHub?.Clients.All.SendAsync("UpdateBackpack", JsonConvert.SerializeObject(Backpack)).Wait();
            }
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            LogLevel = EmpyrionNetAPIDefinitions.LogLevel.Debug;

            Event_Player_Info += PlayerManager_Event_Player_Info;
        }

        private void PlayerManager_Event_Player_Info(PlayerInfo aPlayerInfo)
        {
            UpdateBackpack(aPlayerInfo.steamId, aPlayerInfo.toolbar, aPlayerInfo.bag);
        }

        public void SetPlayerInventory(BackpackModel aSet)
        {
            using (var DB = new PlayerContext())
            {
                var player = DB.Players.FirstOrDefault(P => P.SteamId == aSet.SteamId);
                if (player == null) return;

                var Items = new Inventory(player.EntityId, aSet.Toolbar, aSet.Bag);

                TaskWait.For(2, Request_Player_SetInventory(Items)).Wait();
            }
        }


        public IHubContext<BackpackHub> BackpackHub { get; internal set; }
    }

    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class BackpacksController : ControllerBase
    {
        private BackpackContext _db;

        public BackpackManager BackpackManager { get; }

        public static IEdmModel GetEdmModel()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<Backpack>("Backpacks");
            return builder.GetEdmModel();
        }

        public BackpacksController(BackpackContext context)
        {
            _db = context;
            BackpackManager = Program.GetManager<BackpackManager>();
        }

        [HttpPost("AddItem")]
        public IActionResult AddItem([FromBody]IdItemStack aItem)
        {
            try
            {
                TaskWait.For(2, BackpackManager.Request_Player_AddItem(aItem)).Wait();
                return Ok();
            }
            catch (Exception Error)
            {
                return NotFound(Error.Message);
            }
        }

        [HttpGet("CurrentBackpack/{key}")]
        public IActionResult CurrentBackpack(string key)
        {
            return Ok(_db.Backpacks.FirstOrDefault(B => B.Id == key && B.Timestamp == DateTime.MinValue));
        }

        [HttpGet("Backpacks/{key}")]
        public IActionResult Backpacks(string key)
        {
            return Ok(_db.Backpacks.Where(B => B.Id == key).OrderByDescending(B => B.Timestamp));
        }

        [HttpPost("SetBackpack")]
        public IActionResult SetBackpack([FromBody]BackpackModel aInventory)
        {
            BackpackManager.SetPlayerInventory(aInventory);
            return Ok();
        }

    }
}