using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionModWebHost.Models;
using EmpyrionModWebHost.Services;
using EmpyrionNetAPIAccess;
using EmpyrionNetAPITools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;

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

        public ConfigurationManager<ConcurrentDictionary<int, OfflineWarpPlayerData>> OfflineWarpPlayer { get; set; }
        public static Regex RemoveNameFormatting { get; } = new Regex(@"\[\S+?\]");

        public GameplayManager(IMapper mapper, Microsoft.Extensions.Configuration.IConfiguration configuration, ILogger<GameplayManager> logger)
        {
            Mapper = mapper;
            Configuration = configuration;
            Logger = logger;
            StructureManager = new Lazy<StructureManager>(() => Program.GetManager<StructureManager>());
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
            if(!string.IsNullOrEmpty(idNameMappingFile) && File.Exists(idNameMappingFile)) 
                return JsonConvert
                    .DeserializeObject<Dictionary<string, int>>(File.ReadAllText(idNameMappingFile))
                    .Select(m => new ItemInfo() { Id = m.Value, Name = Localisation.TryGetValue(m.Key, out var Value) ? Value?.Count >= 2 ? Value[1] : m.Key : m.Key })
                    .ToArray();

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
            using var reader = new StringReader(File.ReadAllText(csvFile));
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
