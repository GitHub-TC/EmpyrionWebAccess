using System;
using System.Collections.Generic;
using System.Linq;
using EmpyrionModWebHost.Extensions;
using Eleon.Modding;
using EmpyrionModWebHost.Models;
using EmpyrionNetAPIAccess;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OData.Edm;
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

        public void QueryPlayer(Func<PlayerContext, IEnumerable<Player>> aSelect, Action<Player> aAction)
        {
            using (var DB = new PlayerContext())
            {
                DB.Database.EnsureCreated();
                aSelect(DB).ForEach(P => aAction(P));
            }

        }

        async void UpdatePlayer(Func<PlayerContext, IEnumerable<Player>> aSelect, Action<Player> aChange)
        {
            Player[] ChangedPlayers;
            using (var DB = new PlayerContext())
            {
                DB.Database.EnsureCreated();
                ChangedPlayers = aSelect(DB).ToArray();
                ChangedPlayers.ForEach(P => aChange(P));
                await DB.SaveChangesAsync();
            }

            PlayerHub?.Clients.All.SendAsync("UpdatePlayers", JsonConvert.SerializeObject(ChangedPlayers)).Wait();
        }

        private void PlayerManager_Event_Player_Info(PlayerInfo aPlayerInfo)
        {
            using (var DB = new PlayerContext())
            {
                DB.Database.EnsureCreated();
                var Player = DB.Find<Player>(aPlayerInfo.steamId) ?? new Player();
                var IsNewPlayer = string.IsNullOrEmpty(Player.Id);

                Player.Id = aPlayerInfo.steamId;
                Player.EntityId = aPlayerInfo.entityId;
                Player.SteamId = aPlayerInfo.steamId;
                Player.ClientId = aPlayerInfo.clientId;
                Player.Radiation = aPlayerInfo.radiation;
                Player.RadiationMax = aPlayerInfo.radiationMax;
                Player.BodyTemp = aPlayerInfo.bodyTemp;
                Player.BodyTempMax = aPlayerInfo.bodyTempMax;
                Player.Kills = aPlayerInfo.kills;
                Player.Died = aPlayerInfo.died;
                Player.Credits = aPlayerInfo.credits;
                Player.FoodMax = aPlayerInfo.foodMax;
                Player.Exp = aPlayerInfo.exp;
                Player.Upgrade = aPlayerInfo.upgrade;
                Player.Ping = aPlayerInfo.ping;
                Player.Permission = aPlayerInfo.permission;
                Player.Food = aPlayerInfo.food;
                Player.Stamina = aPlayerInfo.stamina;
                Player.SteamOwnerId = aPlayerInfo.steamOwnerId;
                Player.PlayerName = aPlayerInfo.playerName;
                Player.Playfield = aPlayerInfo.playfield;
                Player.StartPlayfield = aPlayerInfo.startPlayfield;
                Player.StaminaMax = aPlayerInfo.staminaMax;
                Player.FactionGroup = aPlayerInfo.factionGroup;
                Player.FactionId = aPlayerInfo.factionId;
                Player.FactionRole = aPlayerInfo.factionRole;
                Player.Health = aPlayerInfo.health;
                Player.HealthMax = aPlayerInfo.healthMax;
                Player.Oxygen = aPlayerInfo.oxygen;
                Player.OxygenMax = aPlayerInfo.oxygenMax;
                Player.Origin = aPlayerInfo.origin;
                Player.PosX = aPlayerInfo.pos.x;
                Player.PosY = aPlayerInfo.pos.y;
                Player.PosZ = aPlayerInfo.pos.z;
                Player.RotX = aPlayerInfo.rot.x;
                Player.RotY = aPlayerInfo.rot.y;
                Player.RotZ = aPlayerInfo.rot.z;

                if (IsNewPlayer) DB.Players.Add(Player);
                DB.SaveChanges();

                PlayerHub?.Clients.All.SendAsync("UpdatePlayer", JsonConvert.SerializeObject(Player)).Wait();
            }
        }

        public Player GetPlayer(int aPlayerId)
        {
            using (var DB = new PlayerContext())
            {
                DB.Database.EnsureCreated();
                return DB.Players.FirstOrDefault( P => P.EntityId == aPlayerId);
            }
        }

        public int OnlinePlayersCount {
            get {
                using (var DB = new PlayerContext())
                {
                    DB.Database.EnsureCreated();
                    return DB.Players.Count(P => P.Online);
                }
            }
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI  = dediAPI;
            LogLevel = EmpyrionNetAPIDefinitions.LogLevel.Debug;

            Event_Player_Info += PlayerManager_Event_Player_Info;
            Event_Player_Connected += ID =>
               {
                   UpdatePlayer(DB => DB.Players.Where(P => P.EntityId == ID.id), P => P.Online = true);
                   PlayerManager_Event_Player_Info(Request_Player_Info(ID).Result);
               };
            Event_Player_Disconnected   += ID => UpdatePlayer(DB => DB.Players.Where(P => P.EntityId == ID.id), P => P.Online = false);
        }
    }

    public class PlayersController : ODataController
    {
        public PlayerManager PlayerManager { get; }
        public PlayerContext DB { get; }

        public static IEdmModel GetEdmModel()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<Player>("Players");
            return builder.GetEdmModel();
        }


        public PlayersController(PlayerContext aPlayerContext)
        {
            DB = aPlayerContext;
            DB.Database.EnsureCreated();
            PlayerManager = Program.GetManager<PlayerManager>();
        }

        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(DB.Players);
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