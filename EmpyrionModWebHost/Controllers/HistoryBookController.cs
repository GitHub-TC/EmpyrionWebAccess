using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionModWebHost.Models;
using EmpyrionNetAPIAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;

namespace EmpyrionModWebHost.Controllers
{

    public class HistoryBookManager : EmpyrionModBase, IEWAPlugin, IDatabaseConnect
    {
        public ModGameAPI GameAPI { get; private set; }
        public Lazy<StructureManager> StructureManager { get; }
        public Lazy<PlayerManager> PlayerManager { get; }
        public Dictionary<int, PlayfieldStructureData> LastStructuresData { get; } = new Dictionary<int, PlayfieldStructureData>();
        public Dictionary<string, Player> LastPlayerData { get; } = new Dictionary<string, Player>();
        public ExpandoObjectConverter ExpandoObjectConverter { get; } = new ExpandoObjectConverter();

        public void CreateAndUpdateDatabase()
        {
            using (var DB = new HistoryBookContext())
            {
                DB.Database.Migrate();
                DB.Database.EnsureCreated();
            }
        }

        public HistoryBookManager()
        {
            StructureManager = new Lazy<StructureManager>(() => Program.GetManager<StructureManager>());
            PlayerManager    = new Lazy<PlayerManager>   (() => Program.GetManager<PlayerManager>());
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;

            TaskTools.Intervall(30000, HistoryLog);
        }

        private void HistoryLog()
        {
            // Update GlobalSTructureInfo on Playfields
            PlayerManager.Value.QueryPlayer(PDB => 
                PDB.Players
                    .Where(P => P.Online)
                    .GroupBy(P => P.Playfield, (k, p) => p.FirstOrDefault()), 
                    P => {
                        Request_GlobalStructure_Update(new PString(P.Playfield)).Wait();
                        Thread.Sleep(100);
                    });

            using (var DB = new HistoryBookContext())
            {
                var Structures = StructureManager.Value.CurrentGlobalStructures;

                HistoryLogStructures(DB, LastStructuresData, Structures);
                PlayerManager.Value.QueryPlayer(PDB => PDB.Players, P => HistoryLog(DB, LastPlayerData, P));

                DB.SaveChanges();
            }
        }

        private void HistoryLog(HistoryBookContext DB, Dictionary<string, Player> aLastPlayerData, Player aPlayer)
        {
            var Entry = aLastPlayerData.TryGetValue(aPlayer.SteamId, out Player LastData) 
                ? DetectChages(LastData, aPlayer)
                : new HistoryBookOfPlayers()
                {
                    Timestamp = DateTime.Now.ToUniversalTime(),
                    Playfield = aPlayer.Playfield,
                    PosX = (int)aPlayer.PosX,
                    PosY = (int)aPlayer.PosY,
                    PosZ = (int)aPlayer.PosZ,
                    SteamId = aPlayer.SteamId,
                    Name    = aPlayer.PlayerName,
                    Online  = aPlayer.Online,
                    Changed = GetPlayerChanges(null, aPlayer),
                };

            if (!aLastPlayerData.TryAdd(aPlayer.SteamId, aPlayer)) aLastPlayerData[aPlayer.SteamId] = aPlayer;

            if (Entry != null) DB.Players.Add(Entry);
        }

        private HistoryBookOfPlayers DetectChages(Player aLastData, Player aPlayer)
        {
            if (aLastData.Online        == aPlayer.Online &&
                aLastData.Playfield     == aPlayer.Playfield &&
                aLastData.PosX          == aPlayer.PosX &&
                aLastData.PosY          == aPlayer.PosY &&
                aLastData.PosZ          == aPlayer.PosZ &&
                aLastData.Kills         == aPlayer.Kills &&
                aLastData.Died          == aPlayer.Died &&
                aLastData.Credits       == aPlayer.Credits &&
                aLastData.FactionId     == aPlayer.FactionId &&
                aLastData.FactionRole   == aPlayer.FactionRole &&
                aLastData.Permission    == aPlayer.Permission &&
                aLastData.PlayerName    == aPlayer.PlayerName &&
                aLastData.Exp           == aPlayer.Exp &&
                aLastData.Upgrade       == aPlayer.Upgrade
                ) return null;

            return new HistoryBookOfPlayers()
            {
                Timestamp = DateTime.Now.ToUniversalTime(),
                Playfield = aPlayer.Playfield,
                PosX      = (int)aPlayer.PosX,
                PosY      = (int)aPlayer.PosY,
                PosZ      = (int)aPlayer.PosZ,
                SteamId   = aPlayer.SteamId,
                Name      = aPlayer.PlayerName,
                Online    = aPlayer.Online,
                Changed   = GetPlayerChanges(aLastData, aPlayer),
            };

        }

        string GetPlayerChanges(Player aLastData, Player aPlayer)
        {
            dynamic Changes = new ExpandoObject();
            if (aLastData?.Kills         != aPlayer.Kills        ) { Changes.Kills         = aPlayer.Kills;         Changes.KillsOld         = aLastData?.Kills;        }
            if (aLastData?.Died          != aPlayer.Died         ) { Changes.Died          = aPlayer.Died;          Changes.DiedOld          = aLastData?.Died;         }
            if (aLastData?.Credits       != aPlayer.Credits      ) { Changes.Credits       = aPlayer.Credits;       Changes.CreditsOld       = aLastData?.Credits;      }
            if (aLastData?.FactionId     != aPlayer.FactionId    ) { Changes.FactionId     = aPlayer.FactionId;     Changes.FactionIdOld     = aLastData?.FactionId;    }
            if (aLastData?.FactionRole   != aPlayer.FactionRole  ) { Changes.FactionRole   = aPlayer.FactionRole;   Changes.FactionRoleOld   = aLastData?.FactionRole;  }
            if (aLastData?.Permission    != aPlayer.Permission   ) { Changes.Permission    = aPlayer.Permission;    Changes.PermissionOld    = aLastData?.Permission;   }
            if (aLastData?.PlayerName    != aPlayer.PlayerName   ) { Changes.PlayerName    = aPlayer.PlayerName;    Changes.PlayerNameOld    = aLastData?.PlayerName;   }
            if (aLastData?.Exp           != aPlayer.Exp          ) { Changes.Exp           = aPlayer.Exp;           Changes.ExpOld           = aLastData?.Exp;          }
            if (aLastData?.Upgrade       != aPlayer.Upgrade      ) { Changes.Upgrade       = aPlayer.Upgrade;       Changes.UpgradeOld       = aLastData?.Upgrade;      }
            if (aLastData?.Online        != aPlayer.Online       ) { Changes.Online        = aPlayer.Online;        Changes.OnlineOld        = aLastData?.Online;       }
            return JsonConvert.SerializeObject(Changes, ExpandoObjectConverter);
        }

        /// <summary>
        /// classNr == -1 --> Struktur nicht vom DSL geladen
        /// </summary>
        /// <param name="DB"></param>
        /// <param name="aLastStructuresData"></param>
        /// <param name="aStructuresData"></param>
        private void HistoryLogStructures(HistoryBookContext DB, Dictionary<int, PlayfieldStructureData> aLastStructuresData, Dictionary<int, PlayfieldStructureData> aStructuresData)
        {
            bool IsFirstRead = aLastStructuresData.Count == 0;

            // bekannte Strukturen
            aStructuresData
                .Where(S => aLastStructuresData.ContainsKey(S.Key) && S.Value.StructureInfo.classNr != -1 && Changed(aLastStructuresData[S.Key], S.Value))
                .ToArray()
                .ForEach(S =>
                {
                    DB.Structures.Add(CreateStructureHistoryEntry(aLastStructuresData[S.Key].StructureInfo, S.Value, null));
                    aLastStructuresData[S.Key] = S.Value;
                });

            // neue Strukturen
            aStructuresData
                .Where(S => !aLastStructuresData.ContainsKey(S.Key) && S.Value.StructureInfo.classNr != -1)
                .ForEach(S =>
                {
                    DB.Structures.Add(CreateStructureHistoryEntry(null, S.Value, I =>
                    {
                        if (IsFirstRead) I.IsFirstRead = true;
                        else             I.IsNew       = true;
                    }));
                    aLastStructuresData.Add(S.Key, S.Value);
                });

            // gelöschte Strukturen
            aLastStructuresData
                .Where(S => !aStructuresData.ContainsKey(S.Key))
                .ToArray()
                .ForEach(S =>
                {
                    DB.Structures.Add(CreateStructureHistoryEntry(null, S.Value, I => I.IsDeleted = true));
                    aLastStructuresData.Remove(S.Key);
                });
        }

        private bool Changed(PlayfieldStructureData aLastData, PlayfieldStructureData aData)
        {
            return  
                aLastData.Playfield                        != aData.Playfield                        ||
                aLastData.StructureInfo.pos.x              != aData.StructureInfo.pos.x              ||
                aLastData.StructureInfo.pos.y              != aData.StructureInfo.pos.y              ||
                aLastData.StructureInfo.pos.z              != aData.StructureInfo.pos.z              ||
                aLastData.StructureInfo.dockedShips?.Count != aData.StructureInfo.dockedShips?.Count ||
                aLastData.StructureInfo.classNr            != aData.StructureInfo.classNr            ||
                aLastData.StructureInfo.cntLights          != aData.StructureInfo.cntLights          ||
                aLastData.StructureInfo.cntTriangles       != aData.StructureInfo.cntTriangles       ||
                aLastData.StructureInfo.cntBlocks          != aData.StructureInfo.cntBlocks          ||
                aLastData.StructureInfo.cntDevices         != aData.StructureInfo.cntDevices         ||
                aLastData.StructureInfo.powered            != aData.StructureInfo.powered            ||
                aLastData.StructureInfo.name               != aData.StructureInfo.name               ||
                aLastData.StructureInfo.factionId          != aData.StructureInfo.factionId          ||
                aLastData.StructureInfo.factionGroup       != aData.StructureInfo.factionGroup       ||
                aLastData.StructureInfo.coreType           != aData.StructureInfo.coreType           ||
                aLastData.StructureInfo.pilotId            != aData.StructureInfo.pilotId            ||
                aLastData.StructureInfo.type               != aData.StructureInfo.type               ||
                aLastData.StructureInfo.lastVisitedUTC     != aData.StructureInfo.lastVisitedUTC;
        }

        public void DeleteHistory(int aDays)
        {
            var DelTime = DateTime.Now - new TimeSpan(aDays, 0, 0, 0);

            using (var DB = new HistoryBookContext())
            {
                DB.Players   .RemoveRange(DB.Players   .Where(P => P.Timestamp < DelTime));
                DB.Structures.RemoveRange(DB.Structures.Where(S => S.Timestamp < DelTime));

                DB.SaveChanges();
                DB.Database.ExecuteSqlCommand("VACUUM;");
            }
        }

        private HistoryBookOfStructures CreateStructureHistoryEntry(GlobalStructureInfo? aLastData, PlayfieldStructureData aData, Action<dynamic> aAddInfo)
        {
            return new HistoryBookOfStructures()
            {
                Timestamp   = DateTime.Now.ToUniversalTime(),
                Playfield   = aData.Playfield,
                EntityId    = aData.StructureInfo.id,
                Name        = aData.StructureInfo.name,
                PosX        = (int)aData.StructureInfo.pos.x,
                PosY        = (int)aData.StructureInfo.pos.y,
                PosZ        = (int)aData.StructureInfo.pos.z,
                Changed     = GetStructureChanges(aLastData, aData.StructureInfo, aAddInfo),
            };
        }

        private string GetStructureChanges(GlobalStructureInfo? aLastData, GlobalStructureInfo aStructureInfo, Action<dynamic> aAddInfo)
        {
            dynamic Changes = new ExpandoObject();
            if (aLastData?.dockedShips?.Count    != aStructureInfo.dockedShips?.Count) { Changes.DockedShips       = aStructureInfo.dockedShips;     Changes.DockedShipsOld       = aLastData?.dockedShips;     }
            if (aLastData?.classNr               != aStructureInfo.classNr           ) { Changes.ClassNr           = aStructureInfo.classNr;         Changes.ClassNrOld           = aLastData?.classNr;         }
            if (aLastData?.cntLights             != aStructureInfo.cntLights         ) { Changes.CntLights         = aStructureInfo.cntLights;       Changes.CntLightsOld         = aLastData?.cntLights;       }
            if (aLastData?.cntTriangles          != aStructureInfo.cntTriangles      ) { Changes.CntTriangles      = aStructureInfo.cntTriangles;    Changes.CntTrianglesOld      = aLastData?.cntTriangles;    }
            if (aLastData?.cntBlocks             != aStructureInfo.cntBlocks         ) { Changes.CntBlocks         = aStructureInfo.cntBlocks;       Changes.CntBlocksOld         = aLastData?.cntBlocks;       }
            if (aLastData?.cntDevices            != aStructureInfo.cntDevices        ) { Changes.CntDevices        = aStructureInfo.cntDevices;      Changes.CntDevicesOld        = aLastData?.cntDevices;      }
            if (aLastData?.powered               != aStructureInfo.powered           ) { Changes.Powered           = aStructureInfo.powered;         Changes.PoweredOld           = aLastData?.powered;         }
            if (aLastData?.name                  != aStructureInfo.name              ) { Changes.Name              = aStructureInfo.name;            Changes.NameOld              = aLastData?.name;            }
            if (aLastData?.factionId             != aStructureInfo.factionId         ) { Changes.FactionId         = aStructureInfo.factionId;       Changes.FactionIdOld         = aLastData?.factionId;       }
            if (aLastData?.factionGroup          != aStructureInfo.factionGroup      ) { Changes.FactionGroup      = aStructureInfo.factionGroup;    Changes.FactionGroupOld      = aLastData?.factionGroup;    }
            if (aLastData?.coreType              != aStructureInfo.coreType          ) { Changes.CoreType          = aStructureInfo.coreType;        Changes.CoreTypeOld          = aLastData?.coreType;        }
            if (aLastData?.pilotId               != aStructureInfo.pilotId           ) { Changes.PilotId           = aStructureInfo.pilotId;         Changes.PilotIdOld           = aLastData?.pilotId;         }
            if (aLastData?.type                  != aStructureInfo.type              ) { Changes.Type              = aStructureInfo.type;            Changes.TypeOld              = aLastData?.type;            }
            if (aLastData?.lastVisitedUTC        != aStructureInfo.lastVisitedUTC    ) { Changes.LastVisitedUTC    = DateTime.FromBinary(aStructureInfo.lastVisitedUTC);                                        }
            aAddInfo?.Invoke(Changes);
            return JsonConvert.SerializeObject(Changes, ExpandoObjectConverter);       
        }
    }

    [ApiController]
    [Authorize(Roles = nameof(Role.Moderator))]
    [Route("[controller]")]
    public class HistoryBookController : ControllerBase
    {
        public HistoryBookManager HistoryBookManager { get; }
        public HistoryBookContext DB { get; }
        public ExpandoObjectConverter ExpandoObjectConverter { get; } = new ExpandoObjectConverter();

        public HistoryBookController(HistoryBookContext aHistoryBookContext)
        {
            DB = aHistoryBookContext;
            HistoryBookManager = Program.GetManager<HistoryBookManager>();
        }

        public class TimeFrameData
        {
            public DateTime t { get; set; }
            public int distance { get; set; }
            public HistoryBookOfStructures s { get; set; }
            public HistoryBookOfPlayers p { get; set; }

        }

        public class HistoryQuery
        {
            public DateTime FromDateTime { get; set; }
            public DateTime ToDateTime { get; set; }
            public int Distance { get; set; }
            public bool HideOnlyVisited { get; set; }
            public bool HideFirstRead { get; set; }
            public bool HideOnlyPositionChanged { get; set; }
        }

        public class HistoryQueryPlayer : HistoryQuery
        {
            public string SteamId { get; set; }
        }

        public class HistoryQueryStructure : HistoryQuery
        {
            public int Id { get; set; }
        }

        [HttpPost("WhatHappendAroundPlayer")]
        public ActionResult<List<TimeFrameData>> WhatHappendAroundPlayer([FromBody]HistoryQueryPlayer aParams)
        {
            HistoryBookOfPlayers LastKnownPlayer = null;
            string PreviousEntrySteamId = null;

            var TimeFrameData = GetTimeFramedData(aParams.FromDateTime, aParams.ToDateTime);

            List<TimeFrameData> PlayerAround = new List<TimeFrameData>();
            TimeFrameData.ForEach(T =>
            {
                if (T.p?.SteamId == aParams.SteamId) LastKnownPlayer = T.p;
                if (LastKnownPlayer == null) return;
                if (LastKnownPlayer.Playfield != (T.p == null ? T.s.Playfield : T.p.Playfield)) return;
                if (PreviousEntrySteamId != null && aParams.HideOnlyPositionChanged && PreviousEntrySteamId == T.p?.SteamId) return;

                T.distance = CalcDistance(
                    T.p == null ? T.s.PosX : T.p.PosX,
                    T.p == null ? T.s.PosY : T.p.PosY,
                    T.p == null ? T.s.PosZ : T.p.PosZ,
                    LastKnownPlayer.PosX, LastKnownPlayer.PosY, LastKnownPlayer.PosZ);

                if (T.distance > aParams.Distance) return;

                if ((aParams.HideOnlyVisited || aParams.HideFirstRead) && T.s != null && !string.IsNullOrEmpty(T.s.Changed))
                {
                    var Changes = (IDictionary<string, object>)JsonConvert.DeserializeObject<ExpandoObject>(T.s.Changed, ExpandoObjectConverter);
                    if (aParams.HideOnlyVisited && Changes.Count == 1 && Changes.ContainsKey("LastVisitedUTC")) return;
                    if (aParams.HideFirstRead && Changes.ContainsKey("IsFirstRead")) return;
                }

                PreviousEntrySteamId = T.p?.SteamId;
                PlayerAround.Add(T);
            });

            return PlayerAround.OrderByDescending(T => T.t).ToList();
        }

        [HttpPost("WhatHappendAroundStructure")]
        public ActionResult<List<TimeFrameData>> WhatHappendAroundStructure([FromBody]HistoryQueryStructure aParams)
        {
            HistoryBookOfStructures LastKnownStructure = null;
            string PreviousEntrySteamId = null;

            var TimeFrameData = GetTimeFramedData(aParams.FromDateTime, aParams.ToDateTime);

            List<TimeFrameData> StructureAround = new List<TimeFrameData>();
            TimeFrameData.ForEach(T => {
                if (T.s?.EntityId == aParams.Id) LastKnownStructure = T.s;
                if (LastKnownStructure == null) return;
                if (LastKnownStructure.Playfield != (T.p == null ? T.s.Playfield : T.p.Playfield)) return;
                if (PreviousEntrySteamId != null && aParams.HideOnlyPositionChanged && PreviousEntrySteamId == T.p?.SteamId) return;

                T.distance = CalcDistance(
                    T.p == null ? T.s.PosX : T.p.PosX,
                    T.p == null ? T.s.PosY : T.p.PosY,
                    T.p == null ? T.s.PosZ : T.p.PosZ,
                    LastKnownStructure.PosX, LastKnownStructure.PosY, LastKnownStructure.PosZ);

                if (T.distance > aParams.Distance) return;

                if ((aParams.HideOnlyVisited || aParams.HideFirstRead) && T.s != null && !string.IsNullOrEmpty(T.s.Changed))
                {
                    var Changes = (IDictionary<string, object>)JsonConvert.DeserializeObject<ExpandoObject>(T.s.Changed, ExpandoObjectConverter);
                    if (aParams.HideOnlyVisited && Changes.Count == 1 && Changes.ContainsKey("LastVisitedUTC")) return;
                    if (aParams.HideFirstRead && Changes.ContainsKey("IsFirstRead")) return;
                }

                PreviousEntrySteamId = T.p?.SteamId;
                StructureAround.Add(T);
            });

            return StructureAround.OrderByDescending(T => T.t).ToList();
        }

        private List<TimeFrameData> GetTimeFramedData(DateTime FromTime, DateTime ToTime)
        {
            var TimeFrameData = DB.Players.Where(P => P.Timestamp >= FromTime && P.Timestamp <= ToTime).Select(H => new TimeFrameData() { t = H.Timestamp, p = H }).ToList();
            TimeFrameData.AddRange(DB.Structures.Where(S => S.Timestamp >= FromTime && S.Timestamp <= ToTime).Select(H => new TimeFrameData() { t = H.Timestamp, s = H }));

            return TimeFrameData.OrderBy(T => T.t).ToList();
        }

        private int CalcDistance(int posX1, int posY1, int posZ1, int posX2, int posY2, int posZ2)
        {
            return (int)Math.Sqrt(Math.Pow(posX2 - posX1, 2) + Math.Pow(posY2 - posY1, 2) + Math.Pow(posZ2 - posZ1, 2));
        }
    }


}
