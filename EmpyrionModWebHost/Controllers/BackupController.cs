using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionModWebHost.Models;
using EmpyrionNetAPIAccess;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OData.Edm;
using Newtonsoft.Json;

namespace EmpyrionModWebHost.Controllers
{
    public class BackupManager : EmpyrionModBase, IEWAPlugin, IDatabaseConnect
    {
        public ModGameAPI GameAPI { get; private set; }

        public BackupManager()
        {
        }

        public void CreateAndUpdateDatabase()
        {
            //using (var DB = new BackupContext())
            //{
            //    DB.Database.EnsureCreated();
            //}
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            LogLevel = EmpyrionNetAPIDefinitions.LogLevel.Debug;
        }

    }

    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class BackupApiController : ControllerBase
    {
        public BackupManager BackupManager { get; }

        public BackupApiController()
        {
            BackupManager = Program.GetManager<BackupManager>();
        }

        [HttpPost("AddItem")]
        public IActionResult AddItem([FromBody]IdItemStack aItem)
        {
            try
            {
                TaskWait.For(2, BackupManager.Request_Player_AddItem(aItem)).Wait();
                return Ok();
            }
            catch (Exception Error)
            {
                return NotFound(Error.Message);
            }
        }
    }

    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class BackupsController : ControllerBase
    {

        public BackupManager BackupManager { get; }
        public string BackupDir { get; private set; }

        public static IEdmModel GetEdmModel()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            //builder.EntitySet<Backup>("Backups");
            return builder.GetEdmModel();
        }

        public BackupsController()
        {
            BackupDir = Path.Combine(EmpyrionConfiguration.ProgramPath, "Backup");
            BackupManager = Program.GetManager<BackupManager>();
        }


        //        Spawnen von Strukturen:

        //0) Wenn möglich daten(Name, Koordinaten, Typ, Besitzer....aus der Structure-List merken, oder später manuell in die entitySpawnInfo eintragen

        //1) Wenn möglich: Export von Struktur-Infos für späteren Import(Enthält Besitzer, Fuel, Signale, Gruppen, ....)

        //- Request_Entity_Export
        //Parameter: New EntityExportInfo(Entity.ID, [...Savegame\Shared{ Strukturverzeichnis}]Export.dat, boolean LöscheStrukturNachExport))

        //"Export.dat" name ist frei wählbar

        //2) Neue Entity ID besorgen: Request_NewEntityId

        //3) Auf die ID warten bevor man weiter macht
        //4) EntitySpawnInfo füllen
        //a) Wenn vorhanden mit vorher gemerkten Daten aus der Struktur-Liste, ansonsten manuell füllen.
        //Wenn man die Export.dat zur Verfügung hat kann man auch deren Infos nutzen (dann muss hier aber manches auf null oder nothing gesetzt werden. Siehe unten)

        //entitySpawnInfo = New Eleon.Modding.EntitySpawnInfo
        //entitySpawnInfo.forceEntityId = Entity.ID 'Die Neue ID, oder eine alte wenn die frei ist.
        //entitySpawnInfo.playfield = Entity.Playfield
        //If OverwriteCoords Then 'Neue Koordinaten übergeben
        //    entitySpawnInfo.pos = New Eleon.Modding.PVector3(Entity.EW, Entity.Height, Entity.NS)
        //    entitySpawnInfo.rot = New Eleon.Modding.PVector3(Entity.X, Entity.Y, Entity.Z)
        //Else 'Koordinaten aus der Export.dat nutzen
        //    entitySpawnInfo.pos = Nothing
        //    entitySpawnInfo.rot = Nothing
        //End If
        //entitySpawnInfo.name = Entity.Name
        //entitySpawnInfo.type = Entity.GetFromEntityType 'Kommentare der Devs: Set this Undef = 0, BA = 2, CV = 3, SV = 4, HV = 5, AstVoxel = 7
        //'entitySpawnInfo.entityTypeName = "" 'Kommentare der Devs:  ... or set this to f.e. 'ZiraxMale', 'AlienCivilian1Fat', etc

        //If Entity.Typ = eStruct_Type.Other_S‌ructures Then '14 - POI's etc
        //    entitySpawnInfo.prefabName = Entity.File_Name 'Entweder aus der Strukturliste merken oder manuell, siehe unten
        //    entitySpawnInfo.factionGroup = eOwnership.Zirax
        //    entitySpawnInfo.factionId = 0
        //Else 'Normale Strukturen
        //    entitySpawnInfo.prefabName = String.Format("{0}_Player", Sys.GetEnumDescription(Entity.GetFromEntityType))
        //    entitySpawnInfo.factionGroup = Entity.Owner_Typ
        //    entitySpawnInfo.factionId = Entity.Owner_ID
        //End If

        //5) Neues Struktur Verzeichnis vorbereiten und anlegen/leeren

        //Save_GamePath & "Shared" & entitySpawnInfo.prefabName & "" & entitySpawnInfo.forceEntityId

        //6) area dateien + Export.dat wenn vorhanden in dieses neue Verzeichnis kopieren
        //7) Ist eine Export.dat vorhanden dies dem SpawnInfo object mitgeben:

        //entitySpawnInfo.exportedEntityDat = ExportFile(Der ganze Pfad dorthin)

        //8) Playfield laden und abwarten bis es geladen ist

        //Request_Load_Playfield
        //Paramert: New Eleon.Modding.PlayfieldLoad(SecondsToKeepOpen[5 - 20], Playfield, 0)

        //9) Entity Spawnen:
        //Request_Entity_Spawn
        //Parameter: entitySpawnInfo

        //10) Entity berühren(nicht sicher ob das noch gebraucht wird.Früher wurden die sonst sofort gelöscht da die Touch-Zeit leer war.

        //Request_Structure_Touch
        //Parameter: New Eleon.Modding.Id(entity_Id)
        //-----------------------
        //Blueprint Spawnen
        //-----------------------

        //Alles gleich bis auf folgende Steps:
        //1,5,6,7 --> Nicht benötigt
        //Statt 7:
        //eSpanwInfo.prefabName = BlueprintDatei - Name(ohne Dateiendung)

        //Die blueprint Datei muss im Prefab Ordner des Servers liegen (Unter Content\Prefabs oder Content\Scenarios...\Prefabs\
        //--------------------------------
        //Hoffe das hilft.Etwas schwer mit code, da viel anderer Kram enthalten ist oder ich Funktionen wie das warten auf die Entity ID nicht so einfach hier reinpacken kann. Da haste aber vieliecht eh schon selber was

        //Wenn fragen einfach her damit(bearbeitet)
        //Das Entity object ist im prinzip bei mir ein Eintrag in der Struktur liste
        //Das eSpanwInfo oder entitySpawnInfo (sorry ist beides das gleiche) ist von Eleon: Eleon.Modding.EntitySpawnInfo)

        [HttpGet("GetBackups")]
        public IActionResult GetBackups()
        {
            if (!Directory.Exists(BackupDir)) return Ok();
            return Ok(Directory.EnumerateDirectories(BackupDir, "*Backup").OrderByDescending(D => D).Select(D => Path.GetFileName(D)));
        }

        [HttpGet("ReadStructures/{aSelectBackupDir}")]
        public IActionResult ReadStructures(string aSelectBackupDir)
        {
            var StructDir = Path.Combine(BackupDir, aSelectBackupDir, @"Saves\Games", Path.GetFileName(EmpyrionConfiguration.SaveGamePath), "Shared");
            return Ok(Directory.EnumerateFiles(StructDir, "*.txt").AsParallel().Select(I => GenerateGlobalStructureInfo(I)));
        }

        public class PlayfieldGlobalStructureInfo
        {
            public string structureName;
            public string Playfield;
            public int Id;
            public string Name;
            public string Type;
            public int Faction;
            public int Blocks;
            public int Devices;
            public PVector3 Pos;
            public PVector3 Rot;
            public bool Core;
            public bool Powered;
            public bool Docked;
            public DateTime Touched_time;
            public int Touched_ticks;
            public string Touched_name;
            public int Touched_id;
            public DateTime Saved_time;
            public int Saved_ticks;
            public string Add_info;
        }


        private PlayfieldGlobalStructureInfo GenerateGlobalStructureInfo(string aInfoTxtFile)
        {
            var Info = new PlayfieldGlobalStructureInfo();
            Info.structureName = Path.GetFileNameWithoutExtension(aInfoTxtFile);

            try
            {
                var Lines = System.IO.File.ReadAllLines(aInfoTxtFile);
                var FirstLine = Lines.FirstOrDefault();
                var LastLine  = Lines.LastOrDefault();
                if (FirstLine == null || LastLine == null) return Info;

                var FieldNames  = FirstLine.Split(',');
                var FieldValues = LastLine .Split(',');

                string   StringValue    (string N) { var pos = Array.IndexOf(FieldNames, N); return pos == -1 ? null : FieldValues[pos]; }
                int      IntValue       (string N) { var pos = Array.IndexOf(FieldNames, N); return pos == -1 ? 0 : ToIntOrZero(FieldValues[pos]); }
                bool     BoolValue      (string N) { var pos = Array.IndexOf(FieldNames, N); return pos != -1 && bool.TryParse(FieldValues[pos], out bool Result) && Result; }
                PVector3 PVector3Value  (string N) { var pos = Array.IndexOf(FieldNames, N); return pos == -1 ? new PVector3() : GetPVector3(FieldValues[pos]); }
                DateTime DateTimeValue  (string N) { var pos = Array.IndexOf(FieldNames, N); return pos != -1 && DateTime.TryParse(FieldValues[pos], CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime Result) ? Result : DateTime.MinValue; }

                Info.Playfield  = StringValue("playfield");
                Info.Id         = IntValue("id");
                Info.Name       = StringValue("name")?.Trim('\'');
                Info.Type       = StringValue("type");
                Info.Faction    = ToIntOrZero(StringValue("faction")?.Replace("[Fac", "").Replace("]", "").Trim());

                Info.Blocks = IntValue("blocks");
                Info.Devices = IntValue("devices");
                Info.Touched_ticks = IntValue("touched_ticks");
                Info.Touched_id = IntValue("touched_id");
                Info.Saved_ticks = IntValue("saved_ticks");

                Info.Docked = BoolValue("docked");
                Info.Powered = BoolValue("powered");
                Info.Core = BoolValue("core");

                Info.Pos = PVector3Value("pos");
                Info.Rot = PVector3Value("rot");

                Info.Saved_time   = DateTimeValue("saved_time");
                Info.Touched_time = DateTimeValue("touched_time");
                Info.Touched_name = StringValue("touched_name")?.Trim('\'');
                Info.Add_info = StringValue("add_info");
            }
            catch (Exception)
            {
            }

            return Info;
        }

        private PVector3 GetPVector3(string aValue)
        {
            var d = aValue.Split(' ');
            return new PVector3() { x = ToFloatOrZero(d[0]), y = ToFloatOrZero(d[1]), z = ToFloatOrZero(d[2]) };
        }

        private static int ToIntOrZero(string aValue)
        {
            return (int.TryParse(aValue, out int Result) ? Result : 0);
        }

        private static float ToFloatOrZero(string aValue)
        {
            return (float.TryParse(aValue, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float Result) ? Result : 0);
        }

        [HttpPost("CreateStructure/{aSelectBackupDir}")]
        public IActionResult CreateStructure(string aSelectBackupDir, [FromBody]PlayfieldGlobalStructureInfo aStructure)
        {
            return Ok();
        }

    }
}