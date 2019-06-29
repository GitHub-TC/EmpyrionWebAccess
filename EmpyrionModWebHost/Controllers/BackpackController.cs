using System;
using System.Linq;
using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionModWebHost.Models;
using EmpyrionNetAPIAccess;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
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
        public IHubContext<BackpackHub> BackpackHub { get; internal set; }

        public BackpackManager(IHubContext<BackpackHub> aBackpackHub)
        {
            BackpackHub = aBackpackHub;
        }

        public void CreateAndUpdateDatabase()
        {
            using (var DB = new BackpackContext())
            {
                DB.Database.Migrate();
                DB.Database.EnsureCreated();
                DB.Database.ExecuteSqlCommand("PRAGMA journal_mode=WAL;");
            }
        }

        public void DeleteOldBackpacks(int aDays)
        {
            using (var DB = new BackpackContext())
            {
                DB.Backpacks
                    .Where(B => B.Timestamp != DateTime.MinValue && (DateTime.Now - B.Timestamp).TotalDays > aDays)
                    .ToList()
                    .ForEach(B => DB.Backpacks.Remove(B));
                DB.SaveChanges();
                DB.Database.ExecuteSqlCommand("VACUUM;");
            }
        }

        public void UpdateBackpack(string aPlayerSteamId, ItemStack[] aToolbar, ItemStack[] aBag)
        {
            Backpack backpack = TaskTools.Retry(() => backpack = ExecUpdate(aPlayerSteamId, aToolbar, aBag));
            if(backpack != null) BackpackHub?.Clients.All.SendAsync("UpdateBackpack", JsonConvert.SerializeObject(backpack)).Wait();
        }

        private bool IsEqual(ItemStack[] aLeft, ItemStack[] aRight)
        {
            if (aLeft == null && aRight != null) return false;
            if (aLeft != null && aRight == null) return false;
            if (aLeft == null && aRight == null) return true;
            if (aLeft.Length != aRight.Length) return false;

            for (int i = aLeft.Length - 1; i >= 0; i--)
            {
                if (aLeft[i].id    != aRight[i].id   ) return false;
                if (aLeft[i].count != aRight[i].count) return false;
            }

            return true;
        }

        private Backpack ExecUpdate(string aPlayerSteamId, ItemStack[] aToolbar, ItemStack[] aBag)
        {
            using (var DB = new BackpackContext())
            {
                var Backpack = DB.Backpacks
                    .OrderByDescending(B => B.Timestamp)
                    .FirstOrDefault(B => B.Id == aPlayerSteamId);
                var IsNewBackpack = Backpack == null || (DateTime.Now - Backpack.Timestamp).TotalMinutes >= 1;
                var ToolbarContent = JsonConvert.SerializeObject(aToolbar);
                var BagContent = JsonConvert.SerializeObject(aBag);

                if (Backpack != null)
                {
                    if (IsEqual(JsonConvert.DeserializeObject<ItemStack[]>(Backpack.ToolbarContent), aToolbar) &&
                        IsEqual(JsonConvert.DeserializeObject<ItemStack[]>(Backpack.BagContent), aBag)) return null;
                }

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
                if (IsNewBackpack) Backpack = new Backpack()
                {
                    Id          = aPlayerSteamId,
                    Timestamp   = DateTime.MinValue,
                };

                Backpack.ToolbarContent = ToolbarContent;
                Backpack.BagContent = BagContent;
                if (IsNewBackpack) DB.Backpacks.Add(Backpack);

                var count = DB.SaveChanges();

                return Backpack;
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
            if (aPlayerInfo.toolbar?.Length == 0 && aPlayerInfo.bag?.Length == 0)
            {
                BackpackHub?.Clients.All.SendAsync("UpdateBackpack", JsonConvert.SerializeObject(
                    new Backpack() {
                        Id = aPlayerInfo.steamId,
                        ToolbarContent = "[]",
                        BagContent     = "[]",
                    })).Wait();
                return;
            }

            UpdateBackpack(aPlayerInfo.steamId, aPlayerInfo.toolbar, aPlayerInfo.bag);
        }

        public void SetPlayerInventory(BackpackModel aSet)
        {
            using (var DB = new PlayerContext())
            {
                var player = DB.Players.FirstOrDefault(P => P.SteamId == aSet.SteamId);
                if (player == null) return;

                var Items = new Inventory(player.EntityId, aSet.Toolbar, aSet.Bag);

                Request_Player_SetInventory(Items);
            }
        }


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

        [Authorize(Roles = nameof(Role.GameMaster))]
        [HttpPost("AddItem")]
        public IActionResult AddItem([FromBody]IdItemStack aItem)
        {
            try
            {
                BackpackManager.Request_Player_AddItem(aItem);
                TaskWait.Delay(5, () => BackpackManager.Request_Player_Info(new Id(aItem.id)).Wait());
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

        [Authorize(Roles = nameof(Role.GameMaster))]
        [HttpGet("Backpacks/{key}")]
        public IActionResult Backpacks(string key)
        {
            return Ok(_db.Backpacks.Where(B => B.Id == key).OrderByDescending(B => B.Timestamp));
        }

        [Authorize(Roles = nameof(Role.GameMaster))]
        [HttpPost("SetBackpack")]
        public IActionResult SetBackpack([FromBody]BackpackModel aInventory)
        {
            BackpackManager.SetPlayerInventory(aInventory);
            return Ok();
        }

    }
}