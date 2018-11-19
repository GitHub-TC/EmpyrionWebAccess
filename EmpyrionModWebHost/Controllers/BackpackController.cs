using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Eleon.Modding;
using EmpyrionModWebHost.Models;
using EmpyrionNetAPIAccess;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace EmpyrionModWebHost.Controllers
{
    public class BackpackHub : Hub
    {
        private BackpackManager BackpackManager { get; set; }
    }

    public class BackpackManager : EmpyrionModBase, IEWAPlugin
    {
        public ModGameAPI GameAPI { get; private set; }

        public BackpackManager(IHubContext<BackpackHub> aBackpackHub)
        {
            BackpackHub = aBackpackHub;
        }

        public void UpdateBackpack(string aPlayerSteamId, ItemStack[] aBackpackInfo)
        {
            using (var DB = new BackpackContext())
            {
                DB.Database.EnsureCreated();
                var Backpack = DB.Backpacks
                    .OrderByDescending(B => B.timestamp)
                    .FirstOrDefault(B => B.Id == aPlayerSteamId);
                var IsNewBackpack = Backpack == null || (DateTime.Now - Backpack.timestamp).TotalMinutes >= 1;
                var BackpackContent = JsonConvert.SerializeObject(aBackpackInfo);

                if (!IsNewBackpack && Backpack.content == BackpackContent) return;

                if (IsNewBackpack)
                {
                    Backpack = new Backpack()
                    {
                        Id          = aPlayerSteamId,
                        timestamp   = DateTime.Now,
                        content     = BackpackContent
                    };
                    DB.Backpacks.Add(Backpack);
                }

                DB.SaveChanges();

                Backpack = DB.Backpacks.FirstOrDefault(B => B.Id == aPlayerSteamId && B.timestamp == DateTime.MinValue);
                IsNewBackpack = Backpack == null;
                if(IsNewBackpack) Backpack = new Backpack() {
                                                                Id = aPlayerSteamId,
                                                                timestamp = DateTime.MinValue,
                                                            };

                Backpack.content = BackpackContent;
                if (IsNewBackpack) DB.Backpacks.Add(Backpack);

                DB.SaveChanges();

                BackpackHub?.Clients.All.SendAsync("UpdateBackpack", JsonConvert.SerializeObject(Backpack)).Wait();
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

    public class BackpacksController : ODataController
    {
        private BackpackContext _db;

        public BackpackManager BackpackManager { get; }

        public BackpacksController(BackpackContext context)
        {
            _db = context;
            _db.Database.EnsureCreated();
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
            return Ok(_db.Backpacks.FirstOrDefault(B => B.Id == key && B.timestamp == DateTime.MinValue));
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