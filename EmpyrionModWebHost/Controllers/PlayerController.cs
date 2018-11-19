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
    public class PlayerHub : Hub
    {
        private PlayerManager PlayerManager { get; set; }
    }

    public class PlayerManager : EmpyrionModBase, IEWAPlugin
    {
        public IHubContext<PlayerHub> PlayerHub { get; internal set; }
        public ModGameAPI GameAPI { get; private set; }

        public PlayerManager(IHubContext<PlayerHub> aPlayerHub)
        {
            PlayerHub = aPlayerHub;
        }

        private void PlayerManager_Event_Player_Info(PlayerInfo aPlayerInfo)
        {
            using (var DB = new PlayerContext())
            {
                DB.Database.EnsureCreated();
                var Player = DB.Find<Player>(aPlayerInfo.steamId) ?? new Player();
                var IsNewPlayer = string.IsNullOrEmpty(Player.Id);

                Player.Id = aPlayerInfo.steamId;
                Player.entityId = aPlayerInfo.entityId;
                Player.steamId = aPlayerInfo.steamId;
                Player.clientId = aPlayerInfo.clientId;
                Player.radiation = aPlayerInfo.radiation;
                Player.radiationMax = aPlayerInfo.radiationMax;
                Player.bodyTemp = aPlayerInfo.bodyTemp;
                Player.bodyTempMax = aPlayerInfo.bodyTempMax;
                Player.kills = aPlayerInfo.kills;
                Player.died = aPlayerInfo.died;
                Player.credits = aPlayerInfo.credits;
                Player.foodMax = aPlayerInfo.foodMax;
                Player.exp = aPlayerInfo.exp;
                Player.upgrade = aPlayerInfo.upgrade;
                Player.ping = aPlayerInfo.ping;
                Player.permission = aPlayerInfo.permission;
                Player.food = aPlayerInfo.food;
                Player.stamina = aPlayerInfo.stamina;
                Player.steamOwnerId = aPlayerInfo.steamOwnerId;
                Player.playerName = aPlayerInfo.playerName;
                Player.playfield = aPlayerInfo.playfield;
                Player.startPlayfield = aPlayerInfo.startPlayfield;
                Player.staminaMax = aPlayerInfo.staminaMax;
                Player.factionGroup = aPlayerInfo.factionGroup;
                Player.factionId = aPlayerInfo.factionId;
                Player.factionRole = aPlayerInfo.factionRole;
                Player.health = aPlayerInfo.health;
                Player.healthMax = aPlayerInfo.healthMax;
                Player.oxygen = aPlayerInfo.oxygen;
                Player.oxygenMax = aPlayerInfo.oxygenMax;
                Player.origin = aPlayerInfo.origin;
                Player.posX = aPlayerInfo.pos.x;
                Player.posY = aPlayerInfo.pos.y;
                Player.posZ = aPlayerInfo.pos.z;
                Player.rotX = aPlayerInfo.rot.x;
                Player.rotY = aPlayerInfo.rot.y;
                Player.rotZ = aPlayerInfo.rot.z;

                if (IsNewPlayer) DB.Players.Add(Player);
                DB.SaveChanges();

                PlayerHub?.Clients.All.SendAsync("UpdatePlayer", JsonConvert.SerializeObject(Player)).Wait();
            }
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            LogLevel = EmpyrionNetAPIDefinitions.LogLevel.Debug;

            Event_Player_Info += PlayerManager_Event_Player_Info;
        }

    }

    public class PlayersController : ODataController
    {
        private PlayerContext _db;

        public PlayerManager PlayerManager { get; }

        public PlayersController(PlayerContext context)
        {
            _db = context;
            _db.Database.EnsureCreated();
            PlayerManager = Program.GetManager<PlayerManager>();
        }

        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(_db.Players);
        }

        //[EnableQuery]
        //public IActionResult Get(int key)
        //{
        //    return Ok(_db.Players.FirstOrDefault(c => c.entityId == key));
        //}

        //[EnableQuery]
        //public IActionResult Put([FromBody]Player player)
        //{
        //    _db.Players.Add(player);
        //    _db.SaveChanges();
        //    return Created(player);
        //}
    }
}