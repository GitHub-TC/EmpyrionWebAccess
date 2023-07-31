using EmpyrionModWebHost;
using Microsoft.Data.Sqlite;
using System.Data.Common;
using System.Diagnostics;

namespace EgsDbTools
{
    public static class SQLiteExtensions
    {
        public static string SafeGetString(this SqliteDataReader reader, int colIndex) 
            => !reader.IsDBNull(colIndex) ? reader.GetString(colIndex) : string.Empty;
    }
    public class GlobalStructureListAccess
    {
        private GlobalStructureList currentList;

        public DateTime LastDbRead { get; set; }
        public ILogger<GlobalStructureListAccess> Logger { get; }
        public Lazy<SysteminfoManager> SysteminfoManager { get; }
        public int UpdateIntervallInSeconds { get; set; } = 30;

        public string GlobalDbPath { get => _GlobalDbPath; set { if (_GlobalDbPath != value) UpdateNow = true; _GlobalDbPath = value; } }
        string _GlobalDbPath;
        public bool UpdateNow { get; set; }

        public GlobalStructureListAccess(ILogger<GlobalStructureListAccess> logger)
        {
            Logger = logger;
            SysteminfoManager = new Lazy<SysteminfoManager>(() => Program.GetManager<SysteminfoManager>());
        }

        public GlobalStructureList CurrentList
        {
            get {
                if (UpdateNow || (SysteminfoManager.Value.EGSIsRunning && (DateTime.Now - LastDbRead).TotalSeconds > UpdateIntervallInSeconds))
                {
                    var stopwatch = Stopwatch.StartNew();
                    UpdateNow = false;
                    try
                    {
                        currentList = ReadGlobalStructureList();
                    }
                    catch (Exception error)
                    {
                        Logger.LogError(error, "GlobalStructureList CurrentList");
                    }
                    stopwatch.Stop();
                    Logger.LogInformation("GlobalStructureList CurrentList take {stopwatch} for {currentList} structures on {playfields} playfields", stopwatch.Elapsed, currentList?.globalStructures?.Aggregate(0, (c, p) => c + p.Value.Count), currentList?.globalStructures?.Count);

                    LastDbRead = DateTime.Now;
                }
                return currentList;
            }
        }

        private GlobalStructureList ReadGlobalStructureList()
        {
            var gsl = new GlobalStructureList() { globalStructures = new Dictionary<string, List<GlobalStructureInfo>>() };

            var globalStructuresList = new Dictionary<int, GlobalStructureInfo>();
            var dockedToList         = new Dictionary<int, List<int>>();
            SolarSystemData currentPlayfield = null;

            var connectionString = new SqliteConnectionStringBuilder()
            {
                Mode        = SqliteOpenMode.ReadOnly,
                Cache       = SqliteCacheMode.Shared,
                DataSource  = GlobalDbPath
            };

            using (var DbConnection = new SqliteConnection(connectionString.ToString()))
            {
                DbConnection.Open();

                ReadPlayfields(DbConnection);

                using (var cmd = new SqliteCommand(
$@"
SELECT * FROM Structures 
LEFT JOIN Entities ON Structures.entityid = Entities.entityid
ORDER BY pfid
",

//$@"
//SELECT * FROM Structures 
//LEFT JOIN Entities ON Structures.entityid = Entities.entityid
//WHERE Entities.facid > 0 AND (Entities.facgroup = {(int)FactionGroup.Player} OR Entities.facgroup = {(int)FactionGroup.Faction})
//ORDER BY pfid
//",
                DbConnection))
                {
                    List<GlobalStructureInfo> currentPlayfieldStructures = null;
                    int currentPlayfieldId = 0;

                    bool initCols           = true;
                    int pfIdCol             = 0;
                    int entityIdCol         = 0; 
                    int classNrCol          = 0; 
                    int cntLightsCol        = 0; 
                    int cntTrianglesCol     = 0; 
                    int cntBlocksCol        = 0; 
                    int cntDevicesCol       = 0; 
                    int fuelCol             = 0; 
                    int ispoweredCol        = 0; 
                    int pilotidCol          = 0; 
                    int rotXCol             = 0; 
                    int rotYCol             = 0; 
                    int rotZCol             = 0; 
                    int posXCol             = 0; 
                    int posYCol             = 0; 
                    int posZCol             = 0; 
                    int nameCol             = 0; 
                    int facIdCol            = 0; 
                    int lastvisitedticksCol = 0; 
                    int facgroupCol         = 0; 
                    int etypeCol            = 0; 
                    int coretypeCol         = 0; 
                    int dockedToCol         = 0;

                    using(var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (initCols)
                            {
                                initCols = false;

                                pfIdCol             = reader.GetOrdinal("pfid");

                                entityIdCol         = reader.GetOrdinal("entityid");
                                classNrCol          = reader.GetOrdinal("classNr");
                                cntLightsCol        = reader.GetOrdinal("cntLights");
                                cntTrianglesCol     = reader.GetOrdinal("cntTriangles");
                                cntBlocksCol        = reader.GetOrdinal("cntBlocks");
                                cntDevicesCol       = reader.GetOrdinal("cntDevices");
                                fuelCol             = reader.GetOrdinal("fuel");
                                ispoweredCol        = reader.GetOrdinal("ispowered");
                                pilotidCol          = reader.GetOrdinal("pilotid");
                                rotXCol             = reader.GetOrdinal("rotX");
                                rotYCol             = reader.GetOrdinal("rotY");
                                rotZCol             = reader.GetOrdinal("rotZ");
                                posXCol             = reader.GetOrdinal("posX");
                                posYCol             = reader.GetOrdinal("posY");
                                posZCol             = reader.GetOrdinal("posZ");
                                nameCol             = reader.GetOrdinal("name");
                                facIdCol            = reader.GetOrdinal("facid");
                                lastvisitedticksCol = reader.GetOrdinal("lastvisitedticks");
                                facgroupCol         = reader.GetOrdinal("facgroup");
                                etypeCol            = reader.GetOrdinal("etype");
                                coretypeCol         = reader.GetOrdinal("coretype");
                                dockedToCol         = reader.GetOrdinal("dockedTo");
                            }

                            try
                            {
                                int pfid = ReadInt32(reader, pfIdCol);

                                if(pfid != currentPlayfieldId)
                                {
                                    currentPlayfieldId = pfid;
                                    _PlayfieldsById.TryGetValue(pfid, out currentPlayfield);
                                    gsl.globalStructures.Add(_PlayfieldsById[pfid].Name, currentPlayfieldStructures = new List<GlobalStructureInfo>());
                                }

                                var gsi = new GlobalStructureInfo() {
                                    id                  = ReadInt32(reader, entityIdCol),
                                    dockedShips         = new List<int>(),
                                    classNr             = ReadInt32(reader, classNrCol),
                                    cntLights           = ReadInt32(reader, cntLightsCol),
                                    cntTriangles        = ReadInt32(reader, cntTrianglesCol),
                                    cntBlocks           = ReadInt32(reader, cntBlocksCol),
                                    cntDevices          = ReadInt32(reader, cntDevicesCol),
                                    fuel                = (int)ReadFloat(reader, fuelCol),
                                    powered             = ReadBoolean(reader, ispoweredCol),
                                    rot                 = new PVector3(ReadFloat(reader, rotXCol), ReadFloat(reader, rotYCol), ReadFloat(reader, rotZCol)),
                                    pos                 = new PVector3(ReadFloat(reader, posXCol), ReadFloat(reader, posYCol), ReadFloat(reader, posZCol)),
                                    lastVisitedUTC      = ReadInt64(reader, lastvisitedticksCol),
                                    name                = reader.GetValue(nameCol)?.ToString(),
                                    factionId           = ReadInt32(reader, facIdCol),
                                    factionGroup        = ReadByte(reader, facgroupCol),
                                    type                = (byte)(ReadInt32(reader, etypeCol) & 0xff),
                                    coreType            = (sbyte)(ReadInt32(reader, coretypeCol) & 0xff),
                                    pilotId             = reader[pilotidCol] is DBNull ? 0 : ReadInt32(reader, pilotidCol),
                                    PlayfieldName       = currentPlayfield?.Name        ?? "?",
                                    SolarSystemName     = currentPlayfield?.SolarSystem ?? "?"
                                };

                                if (gsi.id == 0) continue;

                                if(!(reader[dockedToCol] is DBNull))
                                {
                                    var dockedTo = ReadInt32(reader, dockedToCol);
                                    if (dockedToList.TryGetValue(dockedTo, out var dockedShips)) dockedShips.Add(gsi.id);
                                    else dockedToList.Add(dockedTo, new List<int> { gsi.id });
                                }

                                globalStructuresList.Add(gsi.id, gsi);
                                currentPlayfieldStructures.Add(gsi);
                            }
                            catch (Exception error)
                            {
                                object[] values = new object [reader.FieldCount];
                                Logger.LogError(error, "GlobalStructureList CurrentList: {@reader} {@values}", reader.GetValues(values), values);
                            }
                        }
                    }
                }

                DbConnection.Close();
            }

            SqliteConnection.ClearAllPools();

            foreach (var item in dockedToList) {
                if(globalStructuresList.TryGetValue(item.Key, out var structure)) structure.dockedShips.AddRange(item.Value);
            }

            return gsl;
        }

        static int ReadInt32(SqliteDataReader reader,  int col) => reader.IsDBNull(col) || reader[col] is DBNull ? 0 : reader.GetInt32(col);
        static long ReadInt64(SqliteDataReader reader, int col) => reader.IsDBNull(col) || reader[col] is DBNull ? 0 : reader.GetInt64(col);
        static byte ReadByte(SqliteDataReader reader,  int col) => reader.IsDBNull(col) || reader[col] is DBNull ? (byte)0 : reader.GetByte(col);
        static float ReadFloat(SqliteDataReader reader, int col) => reader.IsDBNull(col) || reader[col] is DBNull ? 0 : reader.GetFloat(col);
        static bool ReadBoolean(SqliteDataReader reader, int col) => reader.IsDBNull(col) || reader[col] is DBNull ? false : reader.GetBoolean(col);

        public GlobalStructureInfo ReadGlobalStructureInfo(Id id)
        {
            var connectionString = new SqliteConnectionStringBuilder()
            {
                Mode        = SqliteOpenMode.ReadOnly,
                Cache       = SqliteCacheMode.Shared,
                DataSource  = GlobalDbPath
            };

            using (var DbConnection = new SqliteConnection(connectionString.ToString()))
            {
                DbConnection.Open();

                ReadPlayfields(DbConnection);

                using (var cmd = new SqliteCommand(
@"
SELECT * FROM Structures 
LEFT JOIN Entities ON Structures.entityid = Entities.entityid
WHERE Structures.entityid = " + id.id.ToString(),
                DbConnection))
                {
                    bool initCols           = true;
                    int pfIdCol             = 0;
                    int entityIdCol         = 0; 
                    int classNrCol          = 0; 
                    int cntLightsCol        = 0; 
                    int cntTrianglesCol     = 0; 
                    int cntBlocksCol        = 0; 
                    int cntDevicesCol       = 0; 
                    int fuelCol             = 0; 
                    int ispoweredCol        = 0; 
                    int pilotidCol          = 0; 
                    int rotXCol             = 0; 
                    int rotYCol             = 0; 
                    int rotZCol             = 0; 
                    int posXCol             = 0; 
                    int posYCol             = 0; 
                    int posZCol             = 0; 
                    int nameCol             = 0; 
                    int facIdCol            = 0; 
                    int lastvisitedticksCol = 0; 
                    int facgroupCol         = 0; 
                    int etypeCol            = 0; 
                    int coretypeCol         = 0; 
                    int dockedToCol         = 0;

                    using var reader = cmd.ExecuteReader();
                    
                    while (reader.Read())
                    {
                        if (initCols)
                        {
                            initCols = false;

                            pfIdCol             = reader.GetOrdinal("pfid");

                            entityIdCol         = reader.GetOrdinal("entityid");
                            classNrCol          = reader.GetOrdinal("classNr");
                            cntLightsCol        = reader.GetOrdinal("cntLights");
                            cntTrianglesCol     = reader.GetOrdinal("cntTriangles");
                            cntBlocksCol        = reader.GetOrdinal("cntBlocks");
                            cntDevicesCol       = reader.GetOrdinal("cntDevices");
                            fuelCol             = reader.GetOrdinal("fuel");
                            ispoweredCol        = reader.GetOrdinal("ispowered");
                            pilotidCol          = reader.GetOrdinal("pilotid");
                            rotXCol             = reader.GetOrdinal("rotX");
                            rotYCol             = reader.GetOrdinal("rotY");
                            rotZCol             = reader.GetOrdinal("rotZ");
                            posXCol             = reader.GetOrdinal("posX");
                            posYCol             = reader.GetOrdinal("posY");
                            posZCol             = reader.GetOrdinal("posZ");
                            nameCol             = reader.GetOrdinal("name");
                            facIdCol            = reader.GetOrdinal("facid");
                            lastvisitedticksCol = reader.GetOrdinal("lastvisitedticks");
                            facgroupCol         = reader.GetOrdinal("facgroup");
                            etypeCol            = reader.GetOrdinal("etype");
                            coretypeCol         = reader.GetOrdinal("coretype");
                            dockedToCol         = reader.GetOrdinal("dockedTo");
                        }

                        int pfid = reader.GetInt32(pfIdCol);
                        _PlayfieldsById.TryGetValue(pfid, out var currentPlayfield);

                        return new GlobalStructureInfo() {
                            id                  = reader.GetInt32(entityIdCol),
                            dockedShips         = new List<int>(),
                            classNr             = reader.GetInt32(classNrCol),
                            cntLights           = reader.GetInt32(cntLightsCol),
                            cntTriangles        = reader.GetInt32(cntTrianglesCol),
                            cntBlocks           = reader.GetInt32(cntBlocksCol),
                            cntDevices          = reader.GetInt32(cntDevicesCol),
                            fuel                = (int)reader.GetFloat(fuelCol),
                            powered             = reader.GetBoolean(ispoweredCol),
                            rot                 = new PVector3(reader.GetFloat(rotXCol), reader.GetFloat(rotYCol), reader.GetFloat(rotZCol)),
                            pos                 = new PVector3(reader.GetFloat(posXCol), reader.GetFloat(posYCol), reader.GetFloat(posZCol)),
                            lastVisitedUTC      = reader.GetInt64(lastvisitedticksCol),
                            name                = reader.GetValue(nameCol)?.ToString(),
                            factionId           = reader.GetInt32(facIdCol),
                            factionGroup        = reader.GetByte(facgroupCol),
                            type                = (byte)(reader.GetInt32(etypeCol) & 0xff),
                            coreType            = (sbyte)(reader.GetInt32(coretypeCol) & 0xff),
                            pilotId             = reader[pilotidCol] is DBNull ? 0 : reader.GetInt32(pilotidCol),
                            PlayfieldName       = currentPlayfield?.Name        ?? "?",
                            SolarSystemName     = currentPlayfield?.SolarSystem ?? "?"
                        };
                    }
                }

                DbConnection.Close();
            }

            SqliteConnection.ClearAllPools();

            return new GlobalStructureInfo();
        }

        public class SolarSystemData
        {
            public int SsId { get; set; }
            public int PfId { get; set; }
            public string Name { get; set; }
            public string PlayfieldType { get; set; }
            public string SolarSystem { get; set; }
        }

        public DateTime LastDbPlayfieldRead { get; set; }
        public Dictionary<int, SolarSystemData> PlayfieldsById { get { CheckForUpdateReading(); return _PlayfieldsById; } }
        Dictionary<int, SolarSystemData> _PlayfieldsById;
        public Dictionary<string, SolarSystemData> PlayfieldsByName { get { CheckForUpdateReading(); return _PlayfieldsByName; } }
        Dictionary<string, SolarSystemData> _PlayfieldsByName;

        void CheckForUpdateReading()
        {
            if (UpdateNow || (DateTime.Now - LastDbPlayfieldRead).TotalSeconds > UpdateIntervallInSeconds || _PlayfieldsById == null || _PlayfieldsByName == null)
            {
                var connectionString = new SqliteConnectionStringBuilder()
                {
                    Mode       = SqliteOpenMode.ReadOnly,
                    Cache      = SqliteCacheMode.Shared,
                    DataSource = GlobalDbPath
                };

                LastDbPlayfieldRead = DateTime.Now;

                using (var DbConnection = new SqliteConnection(connectionString.ToString()))
                {
                    DbConnection.Open();
                    ReadPlayfields(DbConnection);
                    DbConnection.Close();
                }
            }
        }

        private void ReadPlayfields(SqliteConnection dbConnection)
        {
            LastDbPlayfieldRead = DateTime.Now;

            var result = new Dictionary<int, SolarSystemData>();

            using var cmd = new SqliteCommand(
@"
SELECT Playfields.pfid, Playfields.name, SolarSystems.ssid, SolarSystems.name, Playfields.planettype FROM Playfields
JOIN SolarSystems ON Playfields.ssid = SolarSystems.ssid
",
            dbConnection);

            using var reader = cmd.ExecuteReader();

            while (reader.Read()) result.Add(reader.GetInt32(0),
                new SolarSystemData
                {
                    Name          = reader.SafeGetString(1),
                    SsId          = reader.GetInt32(2),
                    SolarSystem   = reader.SafeGetString(3),
                    PfId          = reader.GetInt32(0),
                    PlayfieldType = reader.SafeGetString(4)
                });

            _PlayfieldsById   = result;
            _PlayfieldsByName = _PlayfieldsById.Values.GroupBy(P => P.Name).ToDictionary(P => P.Key, P => P.First());
        }
    }
}
