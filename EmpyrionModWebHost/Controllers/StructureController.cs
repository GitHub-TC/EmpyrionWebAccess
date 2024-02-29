﻿using AutoMapper;
using EgsDbTools;
using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionModWebHost.Models;
using EmpyrionModWebHost.Services;
using EmpyrionNetAPIAccess;
using EmpyrionNetAPITools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmpyrionModWebHost.Controllers
{

    public class PlayfieldStructureData
    {
        public string Playfield { get; set; }
        public GlobalStructureInfo StructureInfo { get; set; }
    }

    public class GlobalStructureListBackup
    {
        public Tuple<string, GlobalStructureInfo[]>[] Structures { get; set; }
    }

    public class StructureManager : EmpyrionModBase, IEWAPlugin, IDisposable
    {

        public ModGameAPI GameAPI { get; private set; }
        public GlobalStructureListAccess GSLA { get; }
        public IMapper Mapper { get; }
        public ILogger<StructureManager> Logger { get; }
        public ConfigurationManager<GlobalStructureListData> LastGlobalStructureList { get; private set; }
        public string CurrentEBPFile { get; set; }

        public StructureManager(IMapper mapper, ILogger<StructureManager> logger, GlobalStructureListAccess gsla)
        {
            GSLA = gsla;

            Mapper = mapper;
            Logger = logger;
            LastGlobalStructureList = new ConfigurationManager<GlobalStructureListData>()
            {
                ConfigFilename = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "EWA", "DB", "GlobalStructureList.json")
            };
            LastGlobalStructureList.Load();
        }

        public void Dispose()
        {
            try { System.IO.File.Delete(CurrentEBPFile); } catch { }
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;

            API_Exit += () => { try { System.IO.File.Delete(CurrentEBPFile); } catch { } };

            TaskTools.Intervall(Math.Max(1, Program.AppSettings.GlobalStructureUpdateInSeconds) * 1000, () => UpdateGlobalStructureList());
        }


        public GlobalStructureListData GlobalStructureList()
        {
            // falls es noch nicht initialisiert ist, dies jetzt machen
            if(LastGlobalStructureList?.Current?.globalStructures?.Count == 0) UpdateGlobalStructureList();

            return LastGlobalStructureList.Current;
        }

        public GlobalStructureListData UpdateGlobalStructureList()
        {
            try
            {
                GSLA.GlobalDbPath = Path.Combine(EmpyrionConfiguration.SaveGamePath, "global.db");
                GSLA.UpdateNow = true;
                //var gsl = Request_GlobalStructure_List(Timeouts.Wait1m).GetAwaiter().GetResult();
                LastGlobalStructureList.Current = Mapper.Map<GlobalStructureListData>(GSLA.CurrentList);
                TaskTools.Delay(0, () => LastGlobalStructureList.Save(false));
            }
            catch (Exception error)
            {
                Logger.LogInformation(error, "UpdateGlobalStructureList");
            }

            return LastGlobalStructureList.Current;
        }

        public Dictionary<int, PlayfieldStructureData> CurrentGlobalStructures
        {
            get {
                var GS = LastGlobalStructureList.Current?.globalStructures;
                return GS == null
                    ? new Dictionary<int, PlayfieldStructureData>()
                    : GS.Aggregate(new Dictionary<int, PlayfieldStructureData>(), (L, K) =>
                        {
                            K.Value.ForEach(S => L.Add(S.id, new PlayfieldStructureData() { Playfield = K.Key, StructureInfo = Mapper.Map<GlobalStructureInfo>(S) }));
                            return L;
                        });
            }
        }

        public async Task CreateStructureAsync(string aEBPFile, PlayfieldGlobalStructureInfo aStructure)
        {
            var NewID = await Request_NewEntityId();

            aStructure.Type = GetStructureInfo(aEBPFile);

            var SpawnInfo = new EntitySpawnInfo()
            {
                forceEntityId  = NewID.id,
                playfield      = aStructure.Playfield,
                pos            = new PVector3(aStructure.Pos?.x ?? 0, aStructure.Pos?.y ?? 0, aStructure.Pos?.z ?? 0),
                rot            = new PVector3(aStructure.Rot?.x ?? 0, aStructure.Rot?.y ?? 0, aStructure.Rot?.z ?? 0),
                name           = $"EBP:{Path.GetFileNameWithoutExtension(aStructure.Name)}",
                type           = (byte)Array.IndexOf(new[] { "Undef", "", "BA", "CV", "SV", "HV", "", "AstVoxel" }, aStructure.Type), // Entity.GetFromEntityType 'Kommentare der Devs: Set this Undef = 0, BA = 2, CV = 3, SV = 4, HV = 5, AstVoxel = 7
                entityTypeName = "", // 'Kommentare der Devs:  ...or set this to f.e. 'ZiraxMale', 'AlienCivilian1Fat', etc
                prefabName     = Path.GetFileNameWithoutExtension(aEBPFile),
                prefabDir      = Path.GetDirectoryName(aEBPFile),
                factionGroup   = 0,
                factionId      = 0, // erstmal auf "public" aStructure.Faction,
            };

            Logger.LogInformation("EBPFile:{EBPFile} -> {SpawnInfo}", aEBPFile, JsonConvert.SerializeObject(SpawnInfo));

            try { await Request_Load_Playfield(new PlayfieldLoad(20, aStructure.Playfield, 0)); }
            catch { }  // Playfield already loaded

            try
            {
                await Request_Entity_Spawn(SpawnInfo);
                await Request_Structure_Touch(NewID); // Sonst wird die Struktur sofort wieder gelöscht !!!
            }
            catch (Exception error)
            {
                Logger.LogError(error, "CreateStructure EBP failed");
            }
            finally
            {
                try { File.Delete(aEBPFile); } catch { }
            }
        }

        private string GetStructureInfo(string aEBPFile)
        {
            return (File.ReadAllBytes(aEBPFile)[8]) switch
            {
                2   => "BA",
                4   => "SV",
                8   => "CV",
                16  => "HV",
                _   => "BA",
            };
        }
    }

    [Authorize(Roles = nameof(Role.VIP))]
    [ApiController]
    [Route("[controller]")]
    public class StructureController : ControllerBase
    {
        public IUserService UserService { get; }
        public StructureManager StructureManager { get; }
        public PlayerManager PlayerManager { get; }

        public StructureController(IUserService aUserService)
        {
            UserService = aUserService;
            StructureManager = Program.GetManager<StructureManager>();
            PlayerManager = Program.GetManager<PlayerManager>();
        }

        [HttpGet("GlobalStructureList")]
        public IActionResult GlobalStructureList(bool withPois = false)
        {
            if (UserService.CurrentUser.Role == Role.VIP)
            {
                var CurrentPlayer = PlayerManager.CurrentPlayer;
                if (CurrentPlayer == null) return Ok();

                return Ok(StructureManager.GlobalStructureList().globalStructures
                            .SelectMany(P => 
                                P.Value.Where(S =>
                                    (S.factionGroup == (byte)Factions.Faction && S.factionId == CurrentPlayer.FactionId) ||
                                    (S.factionGroup == (byte)Factions.Private && S.factionId == CurrentPlayer.EntityId)
                                )
                            )
                        );
            }


            return Ok(withPois 
                ? StructureManager.GlobalStructureList().globalStructures.SelectMany(P => P.Value) 
                : StructureManager.GlobalStructureList().globalStructures.SelectMany(P => P.Value.Where(S => S.factionGroup == (byte)Factions.Faction || S.factionGroup == (byte)Factions.Private)));
        }

#pragma warning disable IDE1006 // Naming Styles
        public class PlayfieldStructureIdData
        {
            public int id { get; set; }
            public string playfield { get; set; }
        }
#pragma warning restore IDE1006 // Naming Styles

        [HttpPost("DeleteStructures")]
        public IActionResult DeleteStructures([FromBody]PlayfieldStructureIdData[] aEntities)
        {
            var Structures = StructureManager.CurrentGlobalStructures.Where(S => aEntities.Any(I => I.id == S.Key));

            if (UserService.CurrentUser.Role == Role.VIP)
            {
                var CurrentPlayer = PlayerManager.CurrentPlayer;
                if (CurrentPlayer == null) return NotFound();

                var Faction = CurrentPlayer?.FactionId;

                Structures = Structures.Where(S => 
                        (S.Value.StructureInfo.factionGroup == (byte)Factions.Faction && S.Value.StructureInfo.factionId == Faction) ||
                        (S.Value.StructureInfo.factionGroup == (byte)Factions.Private && S.Value.StructureInfo.factionId == CurrentPlayer.EntityId))
                        .ToArray();
            }

            var currentPlayfield = string.Empty;

            Structures
                .OrderBy(S => S.Value.Playfield)
                .ForEach(S =>
                {
                    if (currentPlayfield != S.Value.Playfield)
                    {
                        try
                        {
                            StructureManager.Request_Load_Playfield(new PlayfieldLoad(20, S.Value.Playfield, 0)).Wait();
                            Thread.Sleep(2000); // wait for Playfield finish
                            currentPlayfield = S.Value.Playfield;
                        }
                        catch { }  // Playfield already loaded
                    }
                    StructureManager.Request_Entity_Destroy(new Id(S.Value.StructureInfo.id));
                });
            return Ok();
        }

        [HttpPost("TouchStructures")]
        public IActionResult TouchStructures([FromBody] PlayfieldStructureIdData[] aEntities)
        {
            var Structures = StructureManager.CurrentGlobalStructures.Where(S => aEntities.Any(I => I.id == S.Key));

            if (UserService.CurrentUser.Role == Role.VIP)
            {
                var CurrentPlayer = PlayerManager.CurrentPlayer;
                if (CurrentPlayer == null) return NotFound();

                var Faction = CurrentPlayer?.FactionId;

                Structures = Structures.Where(S =>
                        (S.Value.StructureInfo.factionGroup == (byte)Factions.Faction && S.Value.StructureInfo.factionId == Faction) ||
                        (S.Value.StructureInfo.factionGroup == (byte)Factions.Private && S.Value.StructureInfo.factionId == CurrentPlayer.EntityId))
                        .ToArray();
            }

            var currentPlayfield = string.Empty;

            Structures
                .OrderBy(S => S.Value.Playfield)
                .ForEach(S =>
                {
                    if (currentPlayfield != S.Value.Playfield)
                    {
                        try
                        {
                            StructureManager.Request_Load_Playfield(new PlayfieldLoad(20, S.Value.Playfield, 0)).Wait();
                            Thread.Sleep(2000); // wait for Playfield finish
                            currentPlayfield = S.Value.Playfield;
                        }
                        catch { }  // Playfield already loaded
                    }
                    StructureManager.Request_Structure_Touch(new Id(S.Value.StructureInfo.id));
                });
            return Ok();
        }

        public class SetFactionOfStucturesData
        {
            public string FactionAbbrev { get; set; }
            public int[] EntityIds { get; set; }
        }

        [Authorize(Roles = nameof(Role.Moderator))]
        [HttpPost("SetFactionOfStuctures")]
        public IActionResult SetFactionOfStuctures([FromBody]SetFactionOfStucturesData aData)
        {
            var currentPlayfield = string.Empty;

            aData.EntityIds.ForEach(I =>
            {
                if(StructureManager.CurrentGlobalStructures.TryGetValue(I, out var S))
                {
                    if (currentPlayfield != S.Playfield)
                    {
                        try
                        {
                            StructureManager.Request_Load_Playfield(new PlayfieldLoad(20, S.Playfield, 0)).Wait();
                            Thread.Sleep(2000); // wait for Playfield finish
                            currentPlayfield = S.Playfield;
                        }
                        catch { }  // Playfield already loaded
                    }
                }
                StructureManager.Request_ConsoleCommand(new PString($"faction entity '{aData.FactionAbbrev}' {I}"));
            });
            return Ok();
        }

        public class PlayfieldConsoleCommand
        {
            public string Playfield { get; set; }
            public int EntityId { get; set; }
            public string Command { get; set; }
        }

        [Authorize(Roles = nameof(Role.Moderator))]
        [HttpPost("CallEntity")]
        public async Task CallPlayfieldConsoleCommand([FromBody] PlayfieldConsoleCommand aData)
        {
            var pf = await StructureManager.Request_Playfield_Stats(new PString(aData.Playfield));
            await StructureManager.Request_ConsoleCommand(new PString($"remoteex pf={pf.processId} entity {aData.EntityId} {aData.Command}"));
        }


        [Authorize(Roles = nameof(Role.Moderator))]
        [HttpPost("UploadEBPFile")]
        [DisableRequestSizeLimit]
        public IActionResult UploadEBPFile()
        {
            Program.CreateTempPath();

            foreach (var file in Request.Form.Files)
            {
                try { System.IO.File.Delete(StructureManager.CurrentEBPFile); } catch { }
                StructureManager.CurrentEBPFile = System.IO.Path.GetTempPath() + file.FileName;

                try { Directory.CreateDirectory(Path.GetDirectoryName(StructureManager.CurrentEBPFile)); } catch { }

                using var ToFile = System.IO.File.Create(StructureManager.CurrentEBPFile);
                file.OpenReadStream().CopyTo(ToFile);
            }
            return Ok();
        }

        [Authorize(Roles = nameof(Role.Moderator))]
        [HttpPost("CreateStructure")]
        public IActionResult CreateStructure([FromBody]PlayfieldGlobalStructureInfo aData)
        {
            StructureManager.CreateStructureAsync(StructureManager.CurrentEBPFile, aData).Wait();
            return Ok();
        }

    }
}
