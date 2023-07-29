﻿using EgsDbTools;
using Microsoft.Build.Tasks;
using System.Collections.Concurrent;
using System.Drawing;

namespace EmpyrionModWebHost.Controllers
{

    [Authorize]
    public class PlayerHub : RoleHubBase
    {
    }

    public class PlayerManager : EmpyrionModBase, IEWAPlugin, IDatabaseConnect, IPlayerManager
    {
        public ILogger<PlayerManager> Logger { get; set; }
        public IRoleHubContext<PlayerHub> PlayerHub { get; internal set; }
        public IProvider<IUserService> UserService { get; }
        public GlobalStructureListAccess GSLA { get; }
        public Lazy<SysteminfoManager> SysteminfoManager { get; }
        public Lazy<ChatManager> ChatManager { get; }
        public Lazy<UserManager> UserManager { get; }
        public ModGameAPI GameAPI { get; private set; }
        public ConcurrentDictionary<string, Player> UpdatePlayersQueue { get; set; } = new ConcurrentDictionary<string, Player>();

        public PlayerManager(
            ILogger<PlayerManager> aLogger,
            IRoleHubContext<PlayerHub> aPlayerHub,
            IProvider<IUserService> aUserService,
            GlobalStructureListAccess gsla)
        {
            Logger = aLogger;
            PlayerHub = aPlayerHub;
            UserService = aUserService;
            GSLA = gsla;
            SysteminfoManager = new Lazy<SysteminfoManager>(() => Program.GetManager<SysteminfoManager>());
            ChatManager = new Lazy<ChatManager>(() => Program.GetManager<ChatManager>());
            UserManager = new Lazy<UserManager>(() => Program.GetManager<UserManager>());

            TaskTools.Intervall(10000, SendPlayerUpdates);
        }

        private void SendPlayerUpdates()
        {
            var keys = UpdatePlayersQueue.Keys.ToArray();
            if (keys.Length == 0) return;

            //var updateKey = keys[new Random().Next(0, keys.Length - 1)];
            //UpdatePlayersQueue.TryRemove(updateKey, out var ChangedPlayer);

            PlayerHub?.RoleSendAsync(null, "UpdatePlayers", JsonConvert.SerializeObject(UpdatePlayersQueue.Values.ToArray()));

            UpdatePlayersQueue.Clear();
        }

        public void CreateAndUpdateDatabase()
        {
            using var DB = new PlayerContext();
            DB.Database.Migrate();
            DB.Database.EnsureCreated();
            DB.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        }

        public void QueryPlayer(Func<PlayerContext, IEnumerable<Player>> aSelect, Action<Player> aAction)
        {
            using var DB = new PlayerContext();
            aSelect(DB).ForEach(P => aAction(P));

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

            if (count > 0) ChangedPlayers.ForEach(P => UpdatePlayersQueue.AddOrUpdate(P.Id, P, (S, O) => P));
            //                PlayerHub?.RoleSendAsync(null, "UpdatePlayers", JsonConvert.SerializeObject(ChangedPlayers));
        }

        private async void PlayerManager_Event_Player_Info(PlayerInfo aPlayerInfo)
        {
            Player Player = null;
            try
            {
                using var DB = new PlayerContext();
                Player = DB.Find<Player>(aPlayerInfo.steamId) ?? new Player();
                var IsNewPlayer = string.IsNullOrEmpty(Player.Id);

                Player.Id              = aPlayerInfo.steamId;
                Player.EntityId        = aPlayerInfo.entityId;
                Player.SteamId         = aPlayerInfo.steamId;
                Player.ClientId        = aPlayerInfo.clientId;
                Player.Radiation       = aPlayerInfo.radiation;
                Player.RadiationMax    = aPlayerInfo.radiationMax;
                Player.BodyTemp        = aPlayerInfo.bodyTemp;
                Player.BodyTempMax     = aPlayerInfo.bodyTempMax;
                Player.Kills           = aPlayerInfo.kills;
                Player.Died            = aPlayerInfo.died;
                Player.Credits         = aPlayerInfo.credits;
                Player.FoodMax         = aPlayerInfo.foodMax;
                Player.Exp             = aPlayerInfo.exp;
                Player.Upgrade         = aPlayerInfo.upgrade;
                Player.Ping            = aPlayerInfo.ping;
                Player.Permission      = aPlayerInfo.permission;
                Player.Food            = aPlayerInfo.food;
                Player.Stamina         = aPlayerInfo.stamina;
                Player.SteamOwnerId    = aPlayerInfo.steamOwnerId;
                Player.PlayerName      = aPlayerInfo.playerName;
                Player.Playfield       = aPlayerInfo.playfield;
                Player.SolarSystem     = (GSLA.PlayfieldsByName?.TryGetValue(aPlayerInfo.playfield ?? string.Empty, out var foundSystem) == true ? foundSystem.SolarSystem : string.Empty) ?? string.Empty;
                Player.BpInFactory     = aPlayerInfo.bpInFactory;
                Player.BpRemainingTime = aPlayerInfo.bpRemainingTime;
                Player.StartPlayfield  = aPlayerInfo.startPlayfield;
                Player.StaminaMax      = aPlayerInfo.staminaMax;
                Player.FactionGroup    = aPlayerInfo.factionGroup;
                Player.FactionId       = aPlayerInfo.factionId;
                Player.FactionRole     = aPlayerInfo.factionRole;
                Player.Health          = aPlayerInfo.health;
                Player.HealthMax       = aPlayerInfo.healthMax;
                Player.Oxygen          = aPlayerInfo.oxygen;
                Player.OxygenMax       = aPlayerInfo.oxygenMax;
                Player.Origin          = aPlayerInfo.origin;
                Player.PosX            = aPlayerInfo.pos.x;
                Player.PosY            = aPlayerInfo.pos.y;
                Player.PosZ            = aPlayerInfo.pos.z;
                Player.RotX            = aPlayerInfo.rot.x;
                Player.RotY            = aPlayerInfo.rot.y;
                Player.RotZ            = aPlayerInfo.rot.z;

                if (IsNewPlayer)
                {
                    Player.Note = string.Empty;
                    Player.OnlineTime = new TimeSpan();
                    Player.LastOnline = DateTime.Now;
                    Player.OnlineHours = 0;
                    Player.Online = true;
                    DB.Players.Add(Player);
                }
                var count = await DB.SaveChangesAsync();

                if (count > 0)
                {
                    UpdatePlayersQueue.AddOrUpdate(Player.Id, Player, (S, O) => Player);
                    //PlayerHub?.RoleSendAsync(Player, "UpdatePlayer", JsonConvert.SerializeObject(Player));
                }

                if (IsNewPlayer) SendWelcomeMessage(Player);
            }
            catch(Microsoft.Data.Sqlite.SqliteException sqliteError)
            {
                Logger?.LogError(sqliteError, "PlayerManager_Event_Player_Info:{@aPlayerInfo}->{@Player} {SqliteErrorCode}:{SqliteExtendedErrorCode}", aPlayerInfo, Player, sqliteError.SqliteErrorCode, sqliteError.SqliteExtendedErrorCode);

                if(sqliteError.Message.Contains("database is locked")) System.Environment.Exit(sqliteError.ErrorCode);
            }
            catch (Exception error)
            {
                Logger?.LogError(error, "PlayerManager_Event_Player_Info:{@aPlayerInfo}->{@Player}", aPlayerInfo, Player);
            }
        }

        private void SendWelcomeMessage(Player aPlayer)
        {
            if (string.IsNullOrEmpty(SysteminfoManager.Value.SystemConfig.Current.WelcomeMessage)) return;

            TaskTools.Delay(60, () => _ = ChatManager.Value.ChatMessageADM(string.Format(SysteminfoManager.Value.SystemConfig.Current.WelcomeMessage, aPlayer.PlayerName)));
        }

        public Player GetPlayer(int aPlayerId)
        {
            return TaskTools.Retry(() =>
            {
                using var DB = new PlayerContext();
                return DB.Players.FirstOrDefault(P => P.EntityId == aPlayerId);
            });
        }

        public Player GetPlayer(string aSteamId)
        {
            return TaskTools.Retry(() =>
            {
                using var DB = new PlayerContext();
                return DB.Players.FirstOrDefault(P => P.SteamId == aSteamId);
            });
        }

        public int OnlinePlayersCount
        {
            get
            {
                return TaskTools.Retry(() =>
                {
                    using var DB = new PlayerContext();
                    return DB.Players.Count(P => P.Online);
                });
            }
        }

        public string PlayersDirectory
        {
            get
            {
                Directory.CreateDirectory(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Players"));
                return Path.Combine(EmpyrionConfiguration.SaveGamePath, "Players");
            }
        }

        public FileSystemWatcher mPlayersDirectoryFileWatcher { get; private set; }

        public Player CurrentPlayer
        {
            get
            {
                return TaskTools.Retry(() =>
                {
                    using var DB = new PlayerContext();
                    return DB.Players.Where(P => P.SteamId == UserService.Get().CurrentUser.InGameSteamId).FirstOrDefault();
                });
            }
        }

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
                Filter = "*.ply",
                EnableRaisingEvents = true,
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
                    UpdatePlayer(DB => DB.Players.Where(P => onlinePlayers.list.Contains(P.EntityId) && !P.Online), PlayerConnect);
                    UpdatePlayer(DB => DB.Players.Where(P => !onlinePlayers.list.Contains(P.EntityId) && P.Online), PlayerDisconnect);
                }

                onlinePlayers.list?.AsParallel().ForEach(I => Request_Player_Info(new Id(I)));
            });
        }

        public void SyncronizePlayersWithSaveGameDirectory()
        {
            TaskTools.Delay(1, () =>
            {
                var KnownPlayers = Directory
                    .GetFiles(PlayersDirectory)
                    .Select(F => Path.GetFileNameWithoutExtension(F));

                using var DB = new PlayerContext();
                DB.Players
.Where(P => !KnownPlayers.Contains(P.SteamId))
.ForEach(P => DB.Players.Remove(P));

                DB.SaveChangesAsync();

            });
        }

        public void DeleteOldPlayerFiles(int playerAutoDelete)
        {
            var totalPlayerFiles = Directory.EnumerateFiles(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Players"), "*.ply").ToArray();
            var oldPlayerSteamId = totalPlayerFiles
                .Where(F => (DateTime.Now - File.GetLastWriteTime(F)).TotalDays > playerAutoDelete)
                .ToDictionary(F => Path.GetFileNameWithoutExtension(F), F => F);

            Logger.LogInformation($"Found {totalPlayerFiles.Length} with {oldPlayerSteamId.Count} old player files");

            Dictionary<int, string> oldPlayerEntityId;
            using (var DB = new PlayerContext())
            {
                oldPlayerEntityId = DB.Players
                    .ToArray()
                    .Where(P => oldPlayerSteamId.ContainsKey(P.SteamId))
                    .ToDictionary(P => P.EntityId, P => P.PlayerName);
            }

            oldPlayerSteamId.Values.AsParallel()
            .ForAll(P =>
            {
                try
                {
                    File.Delete(P);
                }
                catch (FileNotFoundException) { }
                catch (Exception error)
                {
                    Logger.LogWarning($"Delete entity {P}: {error}");
                }
            });

            SyncronizePlayersWithSaveGameDirectory();
        }

        private void PlayerDisconnected(Id ID)
        {
            UpdatePlayer(DB => DB.Players.Where(P => P.EntityId == ID.id), PlayerDisconnect);
        }

        private static void PlayerDisconnect(Player aPlayer)
        {
            aPlayer.Online = false;
            aPlayer.OnlineTime += DateTime.Now - aPlayer.LastOnline;
            aPlayer.OnlineHours = (int)Math.Round(aPlayer.OnlineTime.TotalHours);
            aPlayer.LastOnline = DateTime.Now;
        }

        private void PlayerConnected(Id ID)
        {
            UpdatePlayer(DB => DB.Players.Where(P => P.EntityId == ID.id), PlayerConnect);
            TaskTools.Delay(20, () => PlayerManager_Event_Player_Info(Request_Player_Info(ID).Result));
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

                if (aSet.factionRole     .HasValue) player.FactionRole                = aSet.factionRole.Value;
                if (aSet.factionId       .HasValue) player.FactionId                  = aSet.factionId.Value;
                if (aSet.factionGroup    .HasValue) player.FactionGroup               = aSet.factionGroup.Value;
                if (aSet.origin          .HasValue) player.Origin                     = (byte)aSet.origin.Value;
                if (aSet.upgradePoints   .HasValue) player.Upgrade                    = aSet.upgradePoints.Value;
                if (aSet.experiencePoints.HasValue) player.Exp                        = aSet.experiencePoints.Value;
                if (aSet.bodyTempMax     .HasValue) player.BodyTempMax                = aSet.bodyTempMax.Value;
                if (aSet.bodyTemp        .HasValue) player.BodyTemp                   = aSet.bodyTemp.Value;
                if (aSet.radiationMax    .HasValue) player.RadiationMax               = aSet.radiationMax.Value;
                if (aSet.oxygenMax       .HasValue) player.OxygenMax                  = aSet.oxygenMax.Value;
                if (aSet.oxygen          .HasValue) player.Oxygen                     = aSet.oxygen.Value;
                if (aSet.foodMax         .HasValue) player.FoodMax                    = aSet.foodMax.Value;
                if (aSet.food            .HasValue) player.Food                       = aSet.food.Value;
                if (aSet.staminaMax      .HasValue) player.StaminaMax                 = aSet.staminaMax.Value;
                if (aSet.stamina         .HasValue) player.Stamina                    = aSet.stamina.Value;
                if (aSet.healthMax       .HasValue) player.HealthMax                  = aSet.healthMax.Value;
                if (aSet.health          .HasValue) player.Health                     = aSet.health.Value;
                if (!string.IsNullOrEmpty(aSet.startPlayfield)) player.StartPlayfield = aSet.startPlayfield;
                if (aSet.radiation       .HasValue) player.Radiation                  = aSet.radiation.Value;

                DB.SaveChangesAsync();
            }

            Request_Player_SetPlayerInfo(aSet);
        }

        public void ChangePlayerNote(string aSteamId, string aNote)
        {
            UpdatePlayer(DB => DB.Players.Where(P => P.SteamId == aSteamId), P => P.Note = aNote);
        }

        public async void AddConnectionAsync(string aConnectionId, ClaimsPrincipal aUser, IGroupManager aGroups)
        {
            User CurrentUser = UserManager.Value.GetById(int.Parse(aUser.Identity.Name));
            if (CurrentUser.Role <= Role.GameMaster) await aGroups.AddToGroupAsync(aConnectionId, RoleHubBase.AdminsGroupName);
            else
            {
                try
                {
                    await aGroups.AddToGroupAsync(aConnectionId, CurrentUser.InGameSteamId);
                    var CurrentPlayer = GetPlayer(CurrentUser.InGameSteamId);
                    await aGroups.AddToGroupAsync(aConnectionId, CurrentPlayer.FactionId.ToString());
                }
                catch (Exception error)
                {
                    Logger.LogError(error, "AddConnectionAsync:{@aConnectionId} {@aUser} {@aGroups}", aConnectionId, aUser, aGroups);
                }
            }
        }

        public async void RemoveConnectionAsync(string aConnectionId, ClaimsPrincipal aUser, IGroupManager aGroups)
        {
            User CurrentUser = UserManager.Value.GetById(int.Parse(aUser.Identity.Name));
            if (CurrentUser.Role <= Role.GameMaster) await aGroups.RemoveFromGroupAsync(aConnectionId, RoleHubBase.AdminsGroupName);
            else
            {
                try
                {
                    await aGroups.RemoveFromGroupAsync(aConnectionId, CurrentUser.InGameSteamId);
                    var CurrentPlayer = GetPlayer(CurrentUser.InGameSteamId);
                    await aGroups.RemoveFromGroupAsync(aConnectionId, CurrentPlayer.FactionId.ToString());
                }
                catch (Exception error)
                {
                    Logger.LogError(error, "AddConnectionAsync:{@aConnectionId} {@aUser} {@aGroups}", aConnectionId, aUser, aGroups);
                }
            }
        }
    }

    [Authorize]
    public class PlayersController : ODataController
    {
        public PlayerManager PlayerManager { get; }
        public IUserService UserService { get; }
        public PlayerContext DB { get; }

        public static IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<Player>("Players");
            return builder.GetEdmModel();
        }

        public static ODataConventionModelBuilder GetEdmModel(ODataConventionModelBuilder builder)
        {
            builder.EntitySet<Player>("Players");
            return builder;
        }

        public PlayersController(IUserService aUserService, PlayerContext aPlayerContext)
        {
            UserService   = aUserService;
            DB            = aPlayerContext;
            PlayerManager = Program.GetManager<PlayerManager>();
        }

        [EnableQuery]
        public IActionResult Get()
        {
            switch (UserService.CurrentUser.Role)
            {
                case Role.ServerAdmin:
                case Role.InGameAdmin:
                case Role.Moderator:
                case Role.GameMaster:   return Ok(DB.Players);
                case Role.VIP:          var Faction = PlayerManager.CurrentPlayer?.FactionId;
                                        return Ok(DB.Players.Where(P => P.FactionId == Faction));
                case Role.Player:       return Ok(DB.Players.Where(P => P.SteamId == UserService.CurrentUser.InGameSteamId));
                case Role.None: return Ok();
                default:        return Ok();
            }
        }
    }

    [Authorize(Roles = nameof(Role.Moderator))]
    [ApiController]
    [Route("[controller]")]
    public class PlayerController : ControllerBase
    {
        public PlayerManager PlayerManager { get; }

        public PlayerController()
        {
            PlayerManager = Program.GetManager<PlayerManager>();
        }

        [HttpGet("GetElevatedUsers")]
        public IEnumerable<EmpyrionConfiguration.AdminconfigYamlStruct.ElevatedUserStruct> GetElevatedUsers()
        {
            return EmpyrionConfiguration.AdminconfigYaml.ElevatedUsers;
        }

        [HttpGet("GetBannedUsers")]
        public IEnumerable<EmpyrionConfiguration.AdminconfigYamlStruct.BannedUserStruct> GetBannedUsers()
        {
            return EmpyrionConfiguration.AdminconfigYaml.BannedUsers;
        }

        public class SaveNoteData{
            public string SteamId { get; set; }
            public string Note { get; set; }
        }

        [HttpPost("SaveNote")]
        public IActionResult SaveNote([FromBody]SaveNoteData Note)
        {
            PlayerManager.ChangePlayerNote(Note.SteamId, Note.Note);
            return Ok();
        }

        public class PlayerInfoSetDTO
        {
            public int      entityId        { get; set; }
            public byte?    sendLastNLogs   { get; set; }
            public byte?    factionRole     { get; set; }
            public int?     factionId       { get; set; }
            public byte?    factionGroup    { get; set; }
            public int?     origin          { get; set; }
            public int?     upgradePoints   { get; set; }
            public int?     experiencePoints{ get; set; }
            public int?     bodyTempMax     { get; set; }
            public int?     bodyTemp        { get; set; }
            public float?   bpRemainingTime { get; set; }
            public int?     radiationMax    { get; set; }
            public int?     radiation       { get; set; }
            public int?     oxygenMax       { get; set; }
            public int?     oxygen          { get; set; }
            public int?     foodMax         { get; set; }
            public int?     food            { get; set; }
            public int?     staminaMax      { get; set; }
            public int?     stamina         { get; set; }
            public int?     healthMax       { get; set; }
            public int?     health          { get; set; }
            public string   startPlayfield  { get; set; }
        }

        [HttpPost("ChangePlayerInfo")]
        public IActionResult ChangePlayerInfo([FromBody] PlayerInfoSetDTO player)
        {
            var playerSet = new PlayerInfoSet
            {
                entityId         = player.entityId,
                sendLastNLogs    = player.sendLastNLogs,
                factionRole      = player.factionRole,
                factionId        = player.factionId,
                factionGroup     = player.factionGroup,
                origin           = player.origin,
                upgradePoints    = player.upgradePoints,
                experiencePoints = player.experiencePoints,
                bodyTempMax      = player.bodyTempMax,
                bodyTemp         = player.bodyTemp,
                bpRemainingTime  = player.bpRemainingTime,
                radiationMax     = player.radiationMax,
                radiation        = player.radiation,
                oxygenMax        = player.oxygenMax,
                oxygen           = player.oxygen,
                foodMax          = player.foodMax,
                food             = player.food,
                staminaMax       = player.staminaMax,
                stamina          = player.stamina,
                healthMax        = player.healthMax,
                health           = player.health,
                startPlayfield   = player.startPlayfield,
            };

            PlayerManager.ChangePlayerInfo(playerSet);
            return Ok();
        }

        public class PlayerRemoteExDTO
        {
            public int ClientId { get; set; }
            public string Command { get; set; }
        }


        [HttpPost("PlayerRemoteEx")]
        public IActionResult PlayerRemoteEx([FromBody] PlayerRemoteExDTO player)
        {
            PlayerManager.Request_ConsoleCommand(new PString($"remoteex cl={player.ClientId} '{player.Command.Replace("'", "\\'")}'")).GetAwaiter().GetResult();
            return Ok();
        }

    }
}