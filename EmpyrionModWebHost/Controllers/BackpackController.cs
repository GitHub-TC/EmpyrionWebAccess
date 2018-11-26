using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eleon.Modding;
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
            using (var DB = new BackpackContext()) DB.Database.EnsureCreated();
        }

        public void UpdateBackpack(string aPlayerSteamId, ItemStack[] aBackpackInfo)
        {
            using (var DB = new BackpackContext())
            {
                var Backpack = DB.Backpacks
                    .OrderByDescending(B => B.Timestamp)
                    .FirstOrDefault(B => B.Id == aPlayerSteamId);
                var IsNewBackpack = Backpack == null || (DateTime.Now - Backpack.Timestamp).TotalMinutes >= 1;
                var BackpackContent = JsonConvert.SerializeObject(aBackpackInfo);

                if (Backpack?.Content == BackpackContent) return;

                if (IsNewBackpack)
                {
                    Backpack = new Backpack()
                    {
                        Id          = aPlayerSteamId,
                        Timestamp   = DateTime.Now,
                        Content     = BackpackContent
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

                Backpack.Content = BackpackContent;
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
            UpdateBackpack(aPlayerInfo.steamId, aPlayerInfo.toolbar.Concat(aPlayerInfo.bag).ToArray());
        }

        public IHubContext<BackpackHub> BackpackHub { get; internal set; }
    }

    [Authorize]
    public class BackpacksController : ODataController
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

        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(_db.Backpacks);
        }

        [EnableQuery]
        public IActionResult Get(string key)
        {
            return Ok(_db.Backpacks.FirstOrDefault(B => B.Id == key && B.Timestamp == DateTime.MinValue));
        }

        //[EnableQuery]
        //public IActionResult Put([FromBody]Backpack Backpack)
        //{
        //    _db.Backpacks.Add(Backpack);
        //    _db.SaveChanges();
        //    return Created(Backpack);
        //}
    }
}