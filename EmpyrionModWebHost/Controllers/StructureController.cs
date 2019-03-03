using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionModWebHost.Models;
using EmpyrionModWebHost.Services;
using EmpyrionNetAPIAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        public ConfigurationManager<GlobalStructureList> LastGlobalStructureList { get; private set; }
        public string CurrentEBPFile { get; set; }

        public StructureManager()
        {
            LastGlobalStructureList = new ConfigurationManager<GlobalStructureList>()
            {
                UseJSON = true,
                ConfigFilename = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "DB", "GlobalStructureList.json")
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

            TaskTools.Intervall(Math.Max(1, Program.AppSettings.GlobalStructureUpdateInSeconds) * 1000, () => GlobalStructureList());
        }


        public GlobalStructureList GlobalStructureList()
        {
            try
            {
                LastGlobalStructureList.Current = Request_GlobalStructure_List().Result;
                TaskTools.Delay(0, () => LastGlobalStructureList.Save());
            }
            catch { }

            return LastGlobalStructureList.Current;
        }

        public Dictionary<int, PlayfieldStructureData> CurrentGlobalStructures
        {
            get {
                return LastGlobalStructureList.Current.globalStructures
                        .Aggregate(new Dictionary<int, PlayfieldStructureData>(), (L, K) =>
                        {
                            K.Value.ForEach(S => L.Add(S.id, new PlayfieldStructureData() { Playfield = K.Key, StructureInfo = S }));
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
                forceEntityId = NewID.id,
                playfield = aStructure.Playfield,
                pos = aStructure.Pos,
                rot = aStructure.Rot,
                name = $"EBP:{Path.GetFileNameWithoutExtension(aStructure.Name)}",
                type = (byte)Array.IndexOf(new[] { "Undef", "", "BA", "CV", "SV", "HV", "", "AstVoxel" }, aStructure.Type), // Entity.GetFromEntityType 'Kommentare der Devs: Set this Undef = 0, BA = 2, CV = 3, SV = 4, HV = 5, AstVoxel = 7
                entityTypeName = "", // 'Kommentare der Devs:  ...or set this to f.e. 'ZiraxMale', 'AlienCivilian1Fat', etc
                prefabName = Path.GetFileNameWithoutExtension(aEBPFile),
                prefabDir  = Path.GetDirectoryName(aEBPFile),
                factionGroup = 0,
                factionId = 0, // erstmal auf "public" aStructure.Faction,
            };

            try { await Request_Load_Playfield(new PlayfieldLoad(20, aStructure.Playfield, 0)); }
            catch { }  // Playfield already loaded

            try
            {
                await Request_Entity_Spawn(SpawnInfo);
                await Request_Structure_Touch(NewID); // Sonst wird die Struktur sofort wieder gelöscht !!!
            }
            finally
            {
                try { File.Delete(aEBPFile); } catch { }
            }
        }

        private string GetStructureInfo(string aEBPFile)
        {
            switch(File.ReadAllBytes(aEBPFile)[8]){
                default: return "BA";
                case  2: return "BA";
                case  4: return "SV";
                case  8: return "CV";
                case 16: return "HV";
            }
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
        public IActionResult GlobalStructureList()
        {
            if (UserService.CurrentUser.Role == Role.VIP)
            {
                var Result = new GlobalStructureList() { globalStructures = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<GlobalStructureInfo>>() };
                var CurrentPlayer = PlayerManager.CurrentPlayer;
                if (CurrentPlayer == null) return Ok();

                StructureManager.GlobalStructureList().globalStructures
                    .ForEach(P => {
                        var L = P.Value.Where(S =>
                            (S.factionGroup == (byte)Factions.Faction && S.factionId == CurrentPlayer.FactionId) ||
                            (S.factionGroup == (byte)Factions.Private && S.factionId == CurrentPlayer.EntityId)
                        ).ToList();
                        if (L.Count > 0) Result.globalStructures.Add(P.Key, L);
                    });
                return Ok(Result);
            }

            return Ok(StructureManager.GlobalStructureList());
        }

        public class DeleteStructuresData
        {
            public int id { get; set; }
            public string playfield { get; set; }
        }

        [HttpPost("DeleteStructures")]
        public IActionResult DeleteStructures([FromBody]DeleteStructuresData[] aEntities)
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

            Structures
                .OrderBy(S => S.Value.Playfield)
                .ForEach(S =>
                {
                    try
                    {
                        StructureManager.Request_Load_Playfield(new PlayfieldLoad(20, S.Value.Playfield, 0)).Wait();
                        Thread.Sleep(2000); // wait for Playfield finish
                    }
                    catch { }  // Playfield already loaded
                    StructureManager.Request_Entity_Destroy(new Id(S.Value.StructureInfo.id));
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
            aData.EntityIds.ForEach(I => StructureManager.Request_ConsoleCommand(new PString($"faction entity '{aData.FactionAbbrev}' {I}")));
            return Ok();
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

                using (var ToFile = System.IO.File.Create(StructureManager.CurrentEBPFile))
                {
                    file.OpenReadStream().CopyTo(ToFile);
                }
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
