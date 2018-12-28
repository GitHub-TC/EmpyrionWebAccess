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
using Microsoft.AspNetCore.Authorization;
using System.Threading;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace EmpyrionModWebHost.Controllers
{
    [Authorize]
    public class PlayerHub : Hub
    {
        private PlayerManager PlayerManager { get; set; }
    }

    public class PlayerManager : EmpyrionModBase, IEWAPlugin, IDatabaseConnect
    {
        public IHubContext<PlayerHub> PlayerHub { get; internal set; }
        public ModGameAPI GameAPI { get; private set; }
        public PlayerManager(IHubContext<PlayerHub> aPlayerHub)
        {
            PlayerHub = aPlayerHub;
        }

        public void CreateAndUpdateDatabase()
        {
            using (var DB = new PlayerContext())
            {
                DB.Database.Migrate();
                DB.Database.EnsureCreated();
            }
        }

        public void QueryPlayer(Func<PlayerContext, IEnumerable<Player>> aSelect, Action<Player> aAction)
        {
            using (var DB = new PlayerContext())
            {
                aSelect(DB).ForEach(P => aAction(P));
            }

        }

        async void UpdatePlayer(Func<PlayerContext, IEnumerable<Player>> aSelect, Action<Player> aChange)
        {
            Player[] ChangedPlayers;
            int count;
            using (var DB = new PlayerContext())
            {
                ChangedPlayers = aSelect(DB).ToArray();
                ChangedPlayers.ForEach(P => aChange(P));
                count = await DB.SaveChangesAsync();
            }

            if (count > 0) PlayerHub?.Clients.All.SendAsync("UpdatePlayers", JsonConvert.SerializeObject(ChangedPlayers)).Wait();
        }

        private void PlayerManager_Event_Player_Info(PlayerInfo aPlayerInfo)
        {
            using (var DB = new PlayerContext())
            {
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

                if (IsNewPlayer)
                {
                    Player.Note        = string.Empty;
                    Player.OnlineTime  = new TimeSpan();
                    Player.LastOnline  = DateTime.Now;
                    Player.OnlineHours = 0;
                    DB.Players.Add(Player);
                }
                var count = DB.SaveChanges();

                if (count > 0) PlayerHub?.Clients.All.SendAsync("UpdatePlayer", JsonConvert.SerializeObject(Player)).Wait();
            }
        }

        public Player GetPlayer(int aPlayerId)
        {
            using (var DB = new PlayerContext())
            {
                return DB.Players.FirstOrDefault(P => P.EntityId == aPlayerId);
            }
        }

        public int OnlinePlayersCount
        {
            get {
                using (var DB = new PlayerContext())
                {
                    return DB.Players.Count(P => P.Online);
                }
            }
        }

        public string PlayersDirectory {
            get {
                Directory.CreateDirectory(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Players"));
                return Path.Combine(EmpyrionConfiguration.SaveGamePath, "Players");
            }
        }

        public FileSystemWatcher mPlayersDirectoryFileWatcher { get; private set; }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            LogLevel = EmpyrionNetAPIDefinitions.LogLevel.Debug;

            Event_Player_Info += PlayerManager_Event_Player_Info;
            Event_Player_Connected += PlayerConnected;
            Event_Player_Disconnected += PlayerDisconnected;

            UpdateOnlinePlayers();

            SyncronizePlayersWithSaveGameDirectory();

            mPlayersDirectoryFileWatcher = new FileSystemWatcher
            {
                Path = PlayersDirectory,
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite |
                               NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = "*.ply"
            };
            mPlayersDirectoryFileWatcher.Deleted += (s, e) => SyncronizePlayersWithSaveGameDirectory();
        }

        private void UpdateOnlinePlayers()
        {
            TaskTools.Intervall(10000, () =>
            {
                var onlinePlayers = Request_Player_List().Result;
                if (onlinePlayers == null) return;

                if (onlinePlayers.list == null) UpdatePlayer(DB => DB.Players.Where(P => P.Online), PlayerDisconnect);
                else
                {
                    UpdatePlayer(DB => DB.Players.Where(P =>  onlinePlayers.list.Contains(P.EntityId) && !P.Online), PlayerConnect);
                    UpdatePlayer(DB => DB.Players.Where(P => !onlinePlayers.list.Contains(P.EntityId) &&  P.Online), PlayerDisconnect);
                }

                onlinePlayers.list.AsParallel().ForEach(I => Request_Player_Info(new Id(I)));
            });
        }

        private void SyncronizePlayersWithSaveGameDirectory()
        {
            new Thread(() =>
            {
                var KnownPlayers = Directory
                    .GetFiles(PlayersDirectory)
                    .Select(F => Path.GetFileNameWithoutExtension(F));
                using (var DB = new PlayerContext())
                {
                    DB.Players
                        .Where(P => !KnownPlayers.Contains(P.SteamId))
                        .ForEach(P => DB.Players.Remove(P));

                    DB.SaveChanges();
                }

            }).Start();
        }

        private void PlayerDisconnected(Id ID)
        {
            UpdatePlayer(DB => DB.Players.Where(P => P.EntityId == ID.id), PlayerDisconnect);
        }

        private static void PlayerDisconnect(Player aPlayer)
        {
            aPlayer.Online      = false;
            aPlayer.OnlineTime += DateTime.Now - aPlayer.LastOnline;
            aPlayer.OnlineHours = (int)Math.Round(aPlayer.OnlineTime.TotalHours);
            aPlayer.LastOnline  = DateTime.Now;
        }

        private void PlayerConnected(Id ID)
        {
            UpdatePlayer(DB => DB.Players.Where(P => P.EntityId == ID.id), PlayerConnect);
            TaskWait.Delay(20, () => PlayerManager_Event_Player_Info(Request_Player_Info(ID).Result));
        }

        private static void PlayerConnect(Player aPlayer)
        {
            aPlayer.Online = true;
            aPlayer.LastOnline = DateTime.Now;
        }

        public void ChangePlayerInfo(PlayerInfoSet aSet)
        {
            using (var DB = new PlayerContext())
            {
                var player = DB.Players.FirstOrDefault(P => P.EntityId == aSet.entityId);
                if (player == null) return;

                if (aSet.factionRole.HasValue) player.FactionRole = aSet.factionRole.Value;
                if (aSet.factionId.HasValue) player.FactionId = aSet.factionId.Value;
                if (aSet.factionGroup.HasValue) player.FactionGroup = aSet.factionGroup.Value;
                if (aSet.origin.HasValue) player.Origin = (byte)aSet.origin.Value;
                if (aSet.upgradePoints.HasValue) player.Upgrade = aSet.upgradePoints.Value;
                if (aSet.experiencePoints.HasValue) player.Exp = aSet.experiencePoints.Value;
                if (aSet.bodyTempMax.HasValue) player.BodyTempMax = aSet.bodyTempMax.Value;
                if (aSet.bodyTemp.HasValue) player.BodyTemp = aSet.bodyTemp.Value;
                if (aSet.radiationMax.HasValue) player.RadiationMax = aSet.radiationMax.Value;
                if (aSet.oxygenMax.HasValue) player.OxygenMax = aSet.oxygenMax.Value;
                if (aSet.oxygen.HasValue) player.Oxygen = aSet.oxygen.Value;
                if (aSet.foodMax.HasValue) player.FoodMax = aSet.foodMax.Value;
                if (aSet.food.HasValue) player.Food = aSet.food.Value;
                if (aSet.staminaMax.HasValue) player.StaminaMax = aSet.staminaMax.Value;
                if (aSet.stamina.HasValue) player.Stamina = aSet.stamina.Value;
                if (aSet.healthMax.HasValue) player.HealthMax = aSet.healthMax.Value;
                if (aSet.health.HasValue) player.Health = aSet.health.Value;
                if (!string.IsNullOrEmpty(aSet.startPlayfield)) player.StartPlayfield = aSet.startPlayfield;
                if (aSet.radiation.HasValue) player.Radiation = aSet.radiation.Value;

                DB.SaveChanges();
            }

            Request_Player_SetPlayerInfo(aSet);
        }

    }

    [Authorize]
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
            PlayerManager = Program.GetManager<PlayerManager>();
        }

        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(DB.Players);
        }

        [EnableQuery]
        public IActionResult Post([FromBody]PlayerInfoSet player)
        {
            PlayerManager.ChangePlayerInfo(player);
            return Ok();
        }
    }

}