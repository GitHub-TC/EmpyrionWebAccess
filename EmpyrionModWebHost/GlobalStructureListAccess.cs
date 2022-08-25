using Microsoft.Build.Tasks;
using Microsoft.Data.Sqlite;
using System.Diagnostics;

namespace EgsDbTools
{
    public class GlobalStructureListAccess
    {
        private GlobalStructureList currentList;

        public DateTime LastDbRead { get; set; }
        public ILogger<GlobalStructureListAccess> Logger { get; }

        public int UpdateIntervallInSeconds { get; set; } = 30;

        public string GlobalDbPath { get => _GlobalDbPath; set { if (_GlobalDbPath != value) UpdateNow = true; _GlobalDbPath = value; } }
        string _GlobalDbPath;
        public bool UpdateNow { get; set; }

        public GlobalStructureListAccess(ILogger<GlobalStructureListAccess> logger)
        {
            Logger = logger;
        }

        public GlobalStructureList CurrentList
        {
            get {
                if (UpdateNow || (DateTime.Now - LastDbRead).TotalSeconds > UpdateIntervallInSeconds)
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

                currentPlayfields = ReadPlayfields(DbConnection);

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
                                int pfid = reader.GetInt32(pfIdCol);

                                if(pfid != currentPlayfieldId)
                                {
                                    currentPlayfieldId = pfid;
                                    currentPlayfields.TryGetValue(pfid, out currentPlayfield);
                                    gsl.globalStructures.Add(currentPlayfields[pfid].Name, currentPlayfieldStructures = new List<GlobalStructureInfo>());
                                }

                                var gsi = new GlobalStructureInfo() {
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

                                if(!(reader[dockedToCol] is DBNull))
                                {
                                    var dockedTo = reader.GetInt32(dockedToCol);
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

                    DbConnection.Close();
                }
            }

            foreach (var item in dockedToList) {
                if(globalStructuresList.TryGetValue(item.Key, out var structure)) structure.dockedShips.AddRange(item.Value);
            }

            return gsl;
        }

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

                currentPlayfields = ReadPlayfields(DbConnection);

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

                            int pfid = reader.GetInt32(pfIdCol);
                            currentPlayfields.TryGetValue(pfid, out var currentPlayfield);

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
                                type                = reader.GetByte(etypeCol),
                                coreType            = (sbyte)reader.GetByte(coretypeCol),
                                pilotId             = reader[pilotidCol] is DBNull ? 0 : reader.GetInt32(pilotidCol),
                                PlayfieldName       = currentPlayfield?.Name        ?? "?",
                                SolarSystemName     = currentPlayfield?.SolarSystem ?? "?"
                            };
                        }
                    }

                    DbConnection.Close();
                }
            }

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

        public Dictionary<int, SolarSystemData> CurrentPlayfields
        {
            get
            {
                if (UpdateNow || (DateTime.Now - LastDbPlayfieldRead).TotalSeconds > UpdateIntervallInSeconds)
                {
                    var connectionString = new SqliteConnectionStringBuilder()
                    {
                        Mode = SqliteOpenMode.ReadOnly,
                        Cache = SqliteCacheMode.Shared,
                        DataSource = GlobalDbPath
                    };

                    using (var DbConnection = new SqliteConnection(connectionString.ToString()))
                    {
                        DbConnection.Open();
                        currentPlayfields = ReadPlayfields(DbConnection);
                        DbConnection.Close();
                    }
                    LastDbPlayfieldRead = DateTime.Now;
                }
                return currentPlayfields;
            }
        }
        private Dictionary<int, SolarSystemData> currentPlayfields;

        private Dictionary<int, SolarSystemData> ReadPlayfields(SqliteConnection dbConnection)
        {
            var result = new Dictionary<int, SolarSystemData>();

            using (var cmd = new SqliteCommand(
@"
SELECT Playfields.pfid, Playfields.name, SolarSystems.ssid, SolarSystems.name, Playfields.planettype FROM Playfields
JOIN SolarSystems ON Playfields.ssid = SolarSystems.ssid
",
            dbConnection))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) result.Add(reader.GetInt32(0), 
                        new SolarSystemData 
                        { 
                            Name          = reader.GetString(1), 
                            SsId          = reader.GetInt32(2), 
                            SolarSystem   = reader.GetString(3), 
                            PfId          = reader.GetInt32(0), 
                            PlayfieldType = reader.GetString(4) 
                        });
                }
            }

            return result;
        }
    }
}
