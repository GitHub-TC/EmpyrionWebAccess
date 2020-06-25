using Eleon.Modding;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace EgsDbTools
{
    public class GlobalStructureListAccess
    {
        private GlobalStructureList currentList;

        public DateTime LastDbRead { get; set; }

        public int UpdateIntervallInSeconds { get; set; } = 30;

        public string GlobalDbPath { get => _GlobalDbPath; set { if (_GlobalDbPath != value) UpdateNow = true; _GlobalDbPath = value; } }
        string _GlobalDbPath;
        public bool UpdateNow { get; set; }

        public GlobalStructureList CurrentList
        {
            get {
                if (UpdateNow || (DateTime.Now - LastDbRead).TotalSeconds > UpdateIntervallInSeconds)
                {
                    currentList = ReadGlobalStructurList();
                    LastDbRead = DateTime.Now;
                }
                return currentList;
            }
        }

        private GlobalStructureList ReadGlobalStructurList()
        {
            var gsl = new GlobalStructureList() { globalStructures = new Dictionary<string, List<GlobalStructureInfo>>() };

            var globalStructuresList = new Dictionary<int, GlobalStructureInfo>();
            var dockedToList         = new Dictionary<int, List<int>>();

            var connectionString = new SqliteConnectionStringBuilder()
            {
                Mode        = SqliteOpenMode.ReadOnly,
                Cache       = SqliteCacheMode.Shared,
                DataSource  = GlobalDbPath
            };

            using (var DbConnection = new SQLiteConnection(connectionString.ToString()))
            {
                DbConnection.Open();

                Dictionary<int, string> playfields = ReadPlayfields(DbConnection);

                using (var cmd = new SQLiteCommand(DbConnection))
                {
                    cmd.CommandText =
@"
SELECT * FROM Structures 
LEFT JOIN Entities ON Structures.entityid = Entities.entityid
ORDER BY pfid
";

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

                    var reader = cmd.ExecuteReader();
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

                        if(pfid != currentPlayfieldId)
                        {
                            currentPlayfieldId = pfid;
                            gsl.globalStructures.Add(playfields[pfid], currentPlayfieldStructures = new List<GlobalStructureInfo>());
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
                            name                = reader.GetString(nameCol),
                            factionId           = reader.GetInt32(facIdCol),
                            factionGroup        = reader.GetByte(facgroupCol),
                            type                = reader.GetByte(etypeCol),
                            coreType            = (sbyte)reader.GetByte(coretypeCol),
                            pilotId             = reader[pilotidCol] is DBNull ? 0 : reader.GetInt32(pilotidCol),
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
                }
            }

            foreach (var item in dockedToList) globalStructuresList[item.Key].dockedShips.AddRange(item.Value);

            return gsl;
        }

        private Dictionary<int, string> ReadPlayfields(SQLiteConnection dbConnection)
        {
            var result = new Dictionary<int, string>();

            using (var cmd = new SQLiteCommand(dbConnection))
            {
                cmd.CommandText = "SELECT pfid, name FROM Playfields";

                var reader = cmd.ExecuteReader();

                var pfidCol = reader.GetOrdinal("pfid");
                var nameCol = reader.GetOrdinal("name");

                while (reader.Read())
                {
                    result.Add(reader.GetInt32(pfidCol), reader.GetString(nameCol));
                }
            }

            return result;
        }
    }
}
