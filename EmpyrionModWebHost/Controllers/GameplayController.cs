using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Data.SQLite;

namespace EmpyrionModWebHost.Controllers
{

    public class GameplayManager : EmpyrionModBase, IEWAPlugin
    {
        private const string IdDef = "Id:";
        private const string NameDef = "Name:";

        public ModGameAPI GameAPI { get; private set; }
        public IMapper Mapper { get; }
        public Microsoft.Extensions.Configuration.IConfiguration Configuration { get; }
        public ILogger<GameplayManager> Logger { get; }
        public Lazy<StructureManager> StructureManager { get; }
        public Lazy<PlayerManager> PlayerManager { get; }
        public ConfigurationManager<ConcurrentDictionary<int, OfflineWarpPlayerData>> OfflineWarpPlayer { get; set; }
        public static Regex RemoveNameFormatting { get; } = new Regex(@"\[\S+?\]");

        public GameplayManager(IMapper mapper, Microsoft.Extensions.Configuration.IConfiguration configuration, ILogger<GameplayManager> logger)
        {
            Mapper = mapper;
            Configuration = configuration;
            Logger = logger;
            StructureManager = new Lazy<StructureManager>(() => Program.GetManager<StructureManager>());
            PlayerManager    = new Lazy<PlayerManager>(() => Program.GetManager<PlayerManager>());
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;

            OfflineWarpPlayer = new ConfigurationManager<ConcurrentDictionary<int, OfflineWarpPlayerData>>()
            {
                ConfigFilename = Path.Combine(EmpyrionConfiguration.SaveGameModPath, @"EWA\DB\OfflineWarpPlayer.json")
            };

            OfflineWarpPlayer.Load();

            Event_Player_Connected += GameplayManager_Event_Player_Connected;
        }

        public void SaveGameCleanUp(int playerAutoDelete)
        {
            var globalDB = Path.Combine(EmpyrionConfiguration.SaveGamePath, "global.db");

            Logger.LogInformation($"SaveGameCleanUp: {globalDB} PlayerDeleteDays:{playerAutoDelete}");

            using var con = new SQLiteConnection(new SQLiteConnectionStringBuilder()
            {
                JournalMode = SQLiteJournalModeEnum.Off,
                ReadOnly    = false,
                DataSource  = globalDB
            }.ToString());
            con.Open();

            using (var cmd = new SQLiteCommand("PRAGMA journal_mode=OFF", con))
            {
                cmd.ExecuteNonQuery();
            }

            var generatedPlayfields = new Dictionary<string, int>();

            using (var cmd = new SQLiteCommand(@"
SELECT DISTINCT P.pfid, P.name
FROM Playfields P
JOIN Entities E ON P.pfid = E.pfid;
", con))
            {
                using var rdrPlayfields = cmd.ExecuteReader();

                var pfidCol = rdrPlayfields.GetOrdinal("pfid");
                var nameCol = rdrPlayfields.GetOrdinal("name");

                while (rdrPlayfields.Read())
                {
                    generatedPlayfields.Add(rdrPlayfields.GetString(nameCol), rdrPlayfields.GetInt32(pfidCol));
                }

                rdrPlayfields.Close();
            }

            Directory.EnumerateDirectories(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Playfields"))
                .ToList()
                .ForEach(P => generatedPlayfields.Remove(Path.GetFileName(P)));


            Logger.LogInformation($"CleanUpStructures for {generatedPlayfields.Count} unused playfields");

            var deleteEntites = new HashSet<int>();

            using (var cmd = new SQLiteCommand($@"
SELECT entityid FROM Entities WHERE pfid IN ({string.Join(',', generatedPlayfields.Values)})

            ", con))
            {

                using var rdrEntities = cmd.ExecuteReader();

                var entityidCol = rdrEntities.GetOrdinal("entityid");

                while (rdrEntities.Read())
                {
                    deleteEntites.Add(rdrEntities.GetInt32(entityidCol));
                }

                rdrEntities.Close();
            }

            Logger.LogInformation($"CleanUpStructures for {deleteEntites.Count} entities");
            DeleteEntitesInDb(con, deleteEntites);

            deleteEntites.AsParallel()
                .ForAll(E =>
                {
                    try
                    {
                        Directory.Delete(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Shared", E.ToString()), true);
                    }
                    catch (DirectoryNotFoundException) { }
                    catch (Exception error)
                    {
                        Logger.LogWarning($"Delete entity {E}: {error}");
                    }
                });

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

            DeleteEntitesInDb (con, oldPlayerEntityId.Keys);

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

            PlayerManager.Value.SyncronizePlayersWithSaveGameDirectory();

            using (var cmd = new SQLiteCommand("VACUUM", con))
            {
                var vaccumDBSize = new FileInfo(globalDB).Length;

                Logger.LogInformation($"VACUUM START: {vaccumDBSize / (1024 * 1024)}MB");
                var vaccumTimer = Stopwatch.StartNew();
                try { Logger.LogInformation($"VACUUM {cmd.ExecuteNonQuery()}"); }
                catch (Exception error) { Logger.LogError($"SQL:{cmd.CommandText} Error:{error}"); }
                vaccumTimer.Stop();

                Logger.LogInformation($"VACCUM END: take {vaccumTimer.Elapsed} and free {(vaccumDBSize - new FileInfo(globalDB).Length) / (1024 * 1024)}MB");
            }


            con.Close();
        }

        private void DeleteEntitesInDb(SQLiteConnection con, IEnumerable<int> deleteEntites)
        {
            var tables = new List<string>();
            using (var cmd = new SQLiteCommand(@"
SELECT name
FROM sqlite_master
WHERE type='table';
", con))
            {
                using var rdTables = cmd.ExecuteReader();

                var tableCol = rdTables.GetOrdinal("name");

                while (rdTables.Read())
                {
                    tables.Add(rdTables.GetString(tableCol));
                }

                rdTables.Close();
            }

            var directRef = new List<string>();
            var fieldRef = new List<Tuple<string, string>>();

            tables.ForEach(T =>
            {
                using var cmd = new SQLiteCommand($"PRAGMA foreign_key_list({T})", con);
                using var rdForeignKeys = cmd.ExecuteReader();

                var tableCol = rdForeignKeys.GetOrdinal("table");
                var fromCol = rdForeignKeys.GetOrdinal("from");
                var toCol = rdForeignKeys.GetOrdinal("to");

                while (rdForeignKeys.Read())
                {
                    if (rdForeignKeys.GetString(tableCol) == "Entities")
                    {
                        if (rdForeignKeys.GetString(fromCol) == "entityid") directRef.Add(T);
                        else if (rdForeignKeys.GetString(toCol) == "entityid") fieldRef.Add(new Tuple<string, string>(T, rdForeignKeys.GetString(fromCol)));
                    }
                }

                rdForeignKeys.Close();
            });

            directRef.Remove("Entities");

            DeleteEntitesInDbTable("VisitedStructures", "poiid");
            DeleteEntitesInDbTable("TraderHistory", "poiid");
            DeleteEntitesInDbTable("PlayerStatisticsAIVessels", "vesselid");
            DeleteEntitesInDbTable("Marketplace", "stationentityid");
            DeleteEntitesInDbTable("PlayerStatisticsAIVessels", "vesselid");
            DeleteEntitesInDbTable("StationServicesHistory", "stationid");
            DeleteEntitesInDbTable("StationServicesHistory", "shipid");
            DeleteEntitesInDbTable("StationServicesHistory", "playerid");

            ExecSqlInDbTable("PlayerInventoryItems", $"DELETE FROM PlayerInventoryItems WHERE piid IN (SELECT piid FROM PlayerInventory P WHERE P.entityid IN ({string.Join(',', deleteEntites)}))");

            fieldRef.ForEach(R => {
                ExecSqlInDbTable(R.Item1, $"UPDATE {R.Item1} SET {R.Item2} = NULL WHERE {R.Item2} IN ({string.Join(',', deleteEntites)})");
            });
            directRef.ForEach(T => DeleteEntitesInDbTable(T));
            
            //ExecSqlInDbTable("PRAGMA", "PRAGMA foreign_keys = OFF;");
            DeleteEntitesInDbTable("Entities");
            //ExecSqlInDbTable("PRAGMA", "PRAGMA foreign_keys = ON;");

            void DeleteEntitesInDbTable(string tableName, string fieldName = "entityid")
            {
                ExecSqlInDbTable(tableName, $"DELETE FROM {tableName} WHERE {fieldName} IN ({string.Join(',', deleteEntites)})");
            }

            void ExecSqlInDbTable(string tableName, string sql)
            {
                using var cmd = new SQLiteCommand(sql, con);
                Logger.LogInformation($"{cmd.CommandText}");
                try { Logger.LogInformation($" -> {cmd.ExecuteNonQuery()} entities in {tableName} DB"); }
                catch (Exception error) { Logger.LogError($"SQL[{tableName}]:{error}"); }
            }
        }

        private void GameplayManager_Event_Player_Connected(Id player)
        {
            if(OfflineWarpPlayer.Current.TryRemove(player.id, out var warpData))
            {
                OfflineWarpPlayer.Save();
                TaskTools.Delay(Program.AppSettings.PlayerOfflineWarpDelay, () => WarpTo(player.id, warpData.WarpToData));
            }
        }

        static ItemInfo[] _mItemInfo;

        public class WarpToData
        {
            public string Playfield { get; set; }
            public float PosX { get; set; }
            public float PosY { get; set; }
            public float PosZ { get; set; }
            public float RotX { get; set; }
            public float RotY { get; set; }
            public float RotZ { get; set; }
        }

        public class OfflineWarpPlayerData
        {
            public string PlayerName { get; set; }
            public WarpToData WarpToData { get; set; }
        }

        public class PlayfieldStructureInfo
        {
            public string Playfield { get; set; }
            public GlobalStructureInfo Data { get; set; }
        }

        public PlayfieldStructureInfo SearchEntity(GlobalStructureListData aGlobalStructureList, int aSourceId)
        {
            foreach (var TestPlayfieldEntites in aGlobalStructureList.globalStructures)
            {
                var FoundEntity = TestPlayfieldEntites.Value.FirstOrDefault(E => E.id == aSourceId);
                if (FoundEntity.id != 0) return new PlayfieldStructureInfo() { Playfield = TestPlayfieldEntites.Key, Data = Mapper.Map<GlobalStructureInfo>(FoundEntity) };
            }
            return null;
        }

        public IEnumerable<ItemInfo> GetAllItems()
        {
            if (_mItemInfo != null) return _mItemInfo;

            lock (RemoveNameFormatting)
            {
                if (_mItemInfo != null) return _mItemInfo;

                var ItemConfigFile = Path.Combine(EmpyrionConfiguration.ProgramPath, @"Content\Configuration\Config_Example.ecf");
                var LocalizationFile = Path.Combine(EmpyrionConfiguration.ProgramPath, "Content", "Scenarios", EmpyrionConfiguration.DedicatedYaml.CustomScenarioName, @"Extras\Localization.csv");
                if (!File.Exists(LocalizationFile)) LocalizationFile = Path.Combine(EmpyrionConfiguration.ProgramPath, @"Content\Extras\Localization.csv");

                try
                {
                    _mItemInfo = ReadItemInfos(ItemConfigFile, LocalizationFile);
                }
                catch (Exception error)
                {
                    Logger.LogError(error, "Config_Example.ecf: {ItemConfigFile} Localization.csv:{LocalizationFile}", ItemConfigFile, LocalizationFile);
                }

                CreateDummyPNGForUnknownItems(_mItemInfo);
            }

            return _mItemInfo;
        }

        public ItemInfo[] ReadItemInfos(string itemConfigFile, string localizationFile)
        {
            Logger.LogInformation("Config_Example.ecf: {itemConfigFile} Localization.csv:{localizationFile}", itemConfigFile, localizationFile);

            var Localisation = ReadTranslationFromCsv(localizationFile).Aggregate(new Dictionary<string, List<string>>(), (r, d) => {
                if (d?.Count >= 2 && !r.ContainsKey(d[0])) r.Add(d[0], d.Select(name => RemoveNameFormatting.Replace(name, "")).ToList());
                return r; 
            });

            var idNameMappingFile = Configuration?.GetValue<string>("NameIdMappingFile");
            if (!string.IsNullOrEmpty(idNameMappingFile) && File.Exists(idNameMappingFile))
            {
                using (var file = File.OpenRead(idNameMappingFile))
                {
                    return System.Text.Json.JsonSerializer
                        .Deserialize<Dictionary<string, int>>(file)
                        .Select(m => new ItemInfo() { Id = m.Value, Name = Localisation.TryGetValue(m.Key, out var Value) ? Value?.Count >= 2 ? Value[1] : m.Key : m.Key })
                        .ToArray();
                }
            }

            var ItemDef = File.ReadAllLines(itemConfigFile)
                .Where(L => L.Contains(IdDef));

            return ItemDef.Select(L =>
            {
                var IdPos = L.IndexOf(IdDef);
                var IdDelimiter = L.IndexOf(",", IdPos);
                var NamePos = L.IndexOf(NameDef);
                if (NamePos == -1) return null;
                var NameDelimiter = L.IndexOf(",", NamePos);
                if (NameDelimiter == -1) NameDelimiter = L.Length;

                return IdPos >= 0 && NamePos >= 0 && IdDelimiter >= 0
                    ? new ItemInfo()
                    {
                        Id = int.TryParse(L.Substring(IdPos + IdDef.Length, IdDelimiter - IdPos - IdDef.Length), out int Result) ? Result : 0,
                        Name = L.Substring(NamePos + NameDef.Length, NameDelimiter - NamePos - NameDef.Length).Trim()
                    }
                    : null;
            })
            .Select(I =>
            {
                if (I != null && Localisation.TryGetValue(I.Name, out var Value)) I.Name = Value?.Count >= 2 ? Value[1] : I.Name;
                return I;
            })
            .Where(I => I != null)
            .ToArray();
        }

        public List<List<string>> ReadTranslationFromCsv(string csvFile)
        {
            if (!File.Exists(csvFile)) throw new FileNotFoundException("File not found", csvFile);

            var isBadData = false;
            var translations = new List<List<string>>();
            using var reader = File.OpenText(csvFile);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { AllowComments = true, CacheFields = true, IgnoreBlankLines = true, BadDataFound = args => { isBadData = true; LogBadCsvData(args); } });

            csv.Read();
            csv.ReadHeader();
            var languages = csv.HeaderRecord.Length;

            do
            {
                if (!isBadData)
                {
                    var newLine = new List<string>();
                    for (int i = 0; i < languages && csv.TryGetField(typeof(string), i, out var field); i++) newLine.Add(field?.ToString() ?? string.Empty);
                    for (int i = languages - newLine.Count - 1; i >= 0; i--) newLine.Add(string.Empty);

                    translations.Add(newLine);
                }
                isBadData = false;
            }
            while (csv.Read());

            return translations;
        }

        private void LogBadCsvData(BadDataFoundArgs args)
        {
            Logger.LogWarning("Bad CSV Data:\n{RawRecord}\n{Field}", args.RawRecord, args.Field);
        }

        private static void CreateDummyPNGForUnknownItems(ItemInfo[] aItems)
        {
            try
            {
                aItems.AsParallel().ForEach(I =>
                {
                    if (!File.Exists(Path.Combine(@"ClientApp\dist\ClientApp\assets\Items", I.Id + ".png")))
                    {
                        File.Copy(@"ClientApp\dist\ClientApp\assets\Items\0.png",
                                  Path.Combine(@"ClientApp\dist\ClientApp\assets\Items", I.Id + ".png"));
                    }
                });
            }
            catch { }
        }

        public void WipePlayer(string aSteamId)
        {
            Request_ConsoleCommand(new PString($"kick {aSteamId} PlayerWipe"));
            TaskTools.Delay(10, () => File.Delete(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Players", aSteamId + ".ply")));
        }

        internal void WarpTo(int aEntityId, WarpToData aWarpToData)
        {
            var isPlayer = false;
            var isSamePlayfield = false;
            var SourcePlayfield = aWarpToData.Playfield;
            try
            {
                var playerInfo = Request_Player_Info(new Id(aEntityId)).Result;
                isPlayer = true;
                isSamePlayfield = playerInfo.playfield == aWarpToData.Playfield;
            }
            catch{
                // Enities always warp with Request_Entity_ChangePlayfield ?!?
                var structure = SearchEntity(StructureManager.Value.GlobalStructureList(), aEntityId);
                if (structure != null) SourcePlayfield = structure.Playfield;
                isPlayer = false;
                isSamePlayfield = structure.Playfield == aWarpToData.Playfield;
            }

            var pos = new PVector3(aWarpToData.PosX, aWarpToData.PosY, aWarpToData.PosZ);
            var rot = new PVector3(aWarpToData.RotX, aWarpToData.RotY, aWarpToData.RotZ);

            bool WaitForPlayfields = false;
            try
            {
                if (!isSamePlayfield)
                {
                    Request_Load_Playfield(new PlayfieldLoad(20, SourcePlayfield, 0)).Wait();
                    WaitForPlayfields = true;
                }
            }
            catch { }  // Playfield already loaded

            try {
                Request_Load_Playfield(new PlayfieldLoad(20, aWarpToData.Playfield, 0)).Wait();
                WaitForPlayfields = true;
            }
            catch { }  // Playfield already loaded

            if (WaitForPlayfields) Thread.Sleep(2000); // wait for Playfield finish

            if (isSamePlayfield)    Request_Entity_Teleport         (new IdPositionRotation(aEntityId, pos, rot)).Wait();
            else if (isPlayer)      Request_Player_ChangePlayerfield(new IdPlayfieldPositionRotation(aEntityId, aWarpToData.Playfield, pos, rot)).Wait();
            else                    Request_Entity_ChangePlayfield  (new IdPlayfieldPositionRotation(aEntityId, aWarpToData.Playfield, pos, rot)).Wait();

            TaskTools.Delay(10, () => Request_GlobalStructure_Update(new PString(aWarpToData.Playfield)).Wait());
            TaskTools.Delay(15, () => Request_GlobalStructure_Update(new PString(SourcePlayfield)).Wait());

        }

        public void WarpPlayerWhenOnline(int aEntityId, string playerName, WarpToData aWarpToData)
        {
            OfflineWarpPlayer.Current.AddOrUpdate(
                    aEntityId,
                    new OfflineWarpPlayerData()
                    {
                        PlayerName = playerName,
                        WarpToData = aWarpToData
                    },
                    (E, D) => { D.WarpToData = aWarpToData; return D; }
                );

            OfflineWarpPlayer.Save();
        }
    }

    [ApiController]
    [Authorize(Roles = nameof(Role.GameMaster))]
    [Route("[controller]")]
    public class GameplayController : ControllerBase
    {
        public IUserService UserService { get; }
        public GameplayManager GameplayManager { get; }
        public StructureManager StructureManager { get; }
        public PlayerManager PlayerManager { get; }

        public GameplayController(IUserService aUserService)
        {
            UserService = aUserService;
            GameplayManager = Program.GetManager<GameplayManager>();
            StructureManager = Program.GetManager<StructureManager>();
            PlayerManager = Program.GetManager<PlayerManager>();
        }

        [HttpGet("GetAllItems")]
        public IActionResult GetAllItems()
        {
            return Ok(GameplayManager.GetAllItems());
        }

        [HttpPost("WarpTo/{aEntityId}")]
        public IActionResult WarpTo(int aEntityId, [FromBody]GameplayManager.WarpToData aWarpToData)
        {
            var offlinePlayer = IsOfflinePlayer(aEntityId);

            if (offlinePlayer != null) GameplayManager.WarpPlayerWhenOnline(aEntityId, offlinePlayer.PlayerName, aWarpToData);
            else                       GameplayManager.WarpTo              (aEntityId, aWarpToData);

            return Ok();
        }

        private Player IsOfflinePlayer(int aEntityId)
        {
            var player = PlayerManager.GetPlayer(aEntityId);
            return player != null && !player.Online ? player : null;
        }

        [HttpPost("PlayerSetCredits/{aEntityId}/{aCredits}")]
        [Authorize(Roles = nameof(Role.Moderator))]
        public IActionResult PlayerSetCredits(int aEntityId, int aCredits)
        {
            GameplayManager.Request_Player_SetCredits(new IdCredits() { id = aEntityId, credits = aCredits });
            return Ok();
        }

        [HttpGet("KickPlayer/{aSteamId}")]
        public IActionResult KickPlayer(string aSteamId)
        {
            GameplayManager.Request_ConsoleCommand(new PString($"kick {aSteamId} 'you have been kicked from the server'"));
            return Ok();
        }

        [HttpGet("BanPlayer/{aSteamId}/{aDuration}")]
        public IActionResult BanPlayer(string aSteamId, string aDuration)
        {
            switch (UserService.CurrentUser.Role)
            {
                case Role.Moderator:  aDuration = aDuration == "1h" ? aDuration : aDuration == "1d" ? aDuration : "1d"; break;
                case Role.GameMaster: aDuration = "1h"; break;
            }
            GameplayManager.Request_ConsoleCommand(new PString($"ban {aSteamId} {aDuration}"));
            return Ok();
        }

        [HttpGet("SetRoleOfPlayer/{aSteamId}/{aRole}")]
        [Authorize(Roles = nameof(Role.InGameAdmin))]
        public IActionResult SetRoleOfPlayer(string aSteamId, string aRole)
        {
            GameplayManager.Request_ConsoleCommand(new PString($"setrole {aSteamId} {aRole}"));
            return Ok();
        }

        [HttpGet("UnBanPlayer/{aSteamId}")]
        public IActionResult UnBanPlayer(string aSteamId)
        {
            GameplayManager.Request_ConsoleCommand(new PString($"unban {aSteamId}"));
            return Ok();
        }

        [HttpGet("WipePlayer/{aSteamId}")]
        [Authorize(Roles = nameof(Role.Moderator))]
        public IActionResult WipePlayer(string aSteamId)
        {
            GameplayManager.WipePlayer(aSteamId);
            return Ok();
        }


    }
}
