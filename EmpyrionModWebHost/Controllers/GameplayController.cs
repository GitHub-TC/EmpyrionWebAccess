using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionModWebHost.Models;
using EmpyrionNetAPIAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace EmpyrionModWebHost.Controllers
{

    public class GameplayManager : EmpyrionModBase, IEWAPlugin
    {
        private const string IdDef = "Id:";
        private const string NameDef = "Name:";

        public ModGameAPI GameAPI { get; private set; }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
        }

        static ItemInfo[] _mItemInfo;

        public IEnumerable<ItemInfo> GetAllItems()
        {
            if (_mItemInfo != null) return _mItemInfo;

            var ItemDef = File.ReadAllLines(Path.Combine(EmpyrionConfiguration.ProgramPath, @"Content\Configuration\Config_Example.ecf"))
                .Where(L => L.Contains(IdDef));
            var Localisation = File.ReadAllLines(Path.Combine(EmpyrionConfiguration.ProgramPath, @"Content\Extras\Localization.csv"))
                .Where(L => Char.IsLetter(L[0]))
                .Select(L => new { ID= L.Substring(0, L.IndexOf(",")), Name= L.Substring(L.IndexOf(",") + 1) })
                .SafeToDictionary(L => L.ID, L => L.Name, StringComparer.CurrentCultureIgnoreCase);

            _mItemInfo = ItemDef.Select(L =>
            {
                var IdPos = L.IndexOf(IdDef);
                var IdDelimiter = L.IndexOf(",", IdPos);
                var NamePos = L.IndexOf(NameDef);
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
                if (I != null)
                {
                    if (Localisation.TryGetValue(I.Name + ",", out string Value))
                    {
                        var End = Value.IndexOf(",");
                        I.Name = Value.Substring(0, End);
                    }
                }
                return I;
            })
            .Where(I => I != null)
            .ToArray();

            CreateDummyPNGForUnknownItems(_mItemInfo);

            return _mItemInfo;
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
            TaskWait.Delay(10, () => File.Delete(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Players", aSteamId + ".ply")));
        }

    }

    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class GameplayController : ControllerBase
    {
        public GameplayManager GameplayManager { get; }
        public StructureManager StructureManager { get; }

        public GameplayController()
        {
            GameplayManager = Program.GetManager<GameplayManager>();
            StructureManager = Program.GetManager<StructureManager>();
        }

        [HttpGet("GetAllItems")]
        public IActionResult GetAllItems()
        {
            return Ok(GameplayManager.GetAllItems());
        }

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

        public class PlayfieldStructureInfo
        {
            public string Playfield { get; set; }
            public GlobalStructureInfo Data { get; set; }
        }

        public static PlayfieldStructureInfo SearchEntity(GlobalStructureList aGlobalStructureList, int aSourceId)
        {
            foreach (var TestPlayfieldEntites in aGlobalStructureList.globalStructures)
            {
                var FoundEntity = TestPlayfieldEntites.Value.FirstOrDefault(E => E.id == aSourceId);
                if (FoundEntity.id != 0) return new PlayfieldStructureInfo() { Playfield = TestPlayfieldEntites.Key, Data = FoundEntity };
            }
            return null;
        }

        [HttpPost("WarpTo/{aEntityId}")]
        public IActionResult WarpTo(int aEntityId, [FromBody]WarpToData aWarpToData)
        {
            var isPlayer = false;
            var isSamePlayfield = false;
            var SourcePlayfield = aWarpToData.Playfield;
            try
            {
                var playerInfo = GameplayManager.Request_Player_Info(new Id(aEntityId)).Result;
                isPlayer = true;
                isSamePlayfield = playerInfo.playfield == aWarpToData.Playfield;
            }
            catch{
                // Enities always warp with Request_Entity_ChangePlayfield ?!?
                var structure = SearchEntity(StructureManager.GlobalStructureList(), aEntityId);
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
                    GameplayManager.Request_Load_Playfield(new PlayfieldLoad(20, SourcePlayfield, 0)).Wait();
                    WaitForPlayfields = true;
                }
            }
            catch { }  // Playfield already loaded

            try {
                GameplayManager.Request_Load_Playfield(new PlayfieldLoad(20, aWarpToData.Playfield, 0)).Wait();
                WaitForPlayfields = true;
            }
            catch { }  // Playfield already loaded

            if (WaitForPlayfields) Thread.Sleep(2000); // wait for Playfield finish

            if (isSamePlayfield)    GameplayManager.Request_Entity_Teleport         (new IdPositionRotation(aEntityId, pos, rot)).Wait();
            else if (isPlayer)      GameplayManager.Request_Player_ChangePlayerfield(new IdPlayfieldPositionRotation(aEntityId, aWarpToData.Playfield, pos, rot)).Wait();
            else                    GameplayManager.Request_Entity_ChangePlayfield  (new IdPlayfieldPositionRotation(aEntityId, aWarpToData.Playfield, pos, rot)).Wait();

            TaskTools.Delay(10, () => GameplayManager.Request_GlobalStructure_Update(new PString(aWarpToData.Playfield)).Wait());
            TaskTools.Delay(15, () => GameplayManager.Request_GlobalStructure_Update(new PString(SourcePlayfield)).Wait());

            return Ok();
        }

        [HttpPost("PlayerSetCredits/{aEntityId}/{aCredits}")]
        public IActionResult PlayerSetCredits(int aEntityId, int aCredits)
        {
            GameplayManager.Request_Player_SetCredits(new IdCredits() { id = aEntityId, credits = aCredits });
            return Ok();
        }

        [HttpGet("BanPlayer/{aSteamId}/{aDuration}")]
        public IActionResult BanPlayer(string aSteamId, string aDuration)
        {
            GameplayManager.Request_ConsoleCommand(new PString($"ban {aSteamId} {aDuration}"));
            return Ok();
        }

        [HttpGet("UnBanPlayer/{aSteamId}")]
        public IActionResult UnBanPlayer(string aSteamId)
        {
            GameplayManager.Request_ConsoleCommand(new PString($"unban {aSteamId}"));
            return Ok();
        }

        [HttpGet("WipePlayer/{aSteamId}")]
        public IActionResult WipePlayer(string aSteamId)
        {
            GameplayManager.WipePlayer(aSteamId);
            return Ok();
        }


    }
}
