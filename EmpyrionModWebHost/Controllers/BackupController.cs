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
    public class BackupManager : EmpyrionModBase, IEWAPlugin
    {
        public ModGameAPI GameAPI { get; private set; }
        public Lazy<SysteminfoManager> SysteminfoManager { get; }
        public string BackupDir { get; internal set; }
        public string CurrentBackupDirectory {
            get {
                var Result = Path.Combine(BackupDir, $"{DateTime.Now.ToString("yyyyMMdd HHmm")} Backup");
                Directory.CreateDirectory(Result);

                return Result;
            }
        }

        public BackupManager()
        {
            SysteminfoManager = new Lazy<SysteminfoManager>(() => Program.GetManager<SysteminfoManager>());

            BackupDir = Path.Combine(EmpyrionConfiguration.ProgramPath, "Saves", Program.AppSettings.BackupDirectory ?? "Backup");
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            LogLevel = EmpyrionNetAPIDefinitions.LogLevel.Debug;
        }

        private void CopyAll(DirectoryInfo aSource, DirectoryInfo aTarget)
        {
            aSource.GetDirectories().AsParallel().ForEach(D => CopyAll(D, aTarget.CreateSubdirectory(D.Name)));
            aSource.GetFiles().AsParallel().ForEach(F => {
                Directory.CreateDirectory(aTarget.FullName);
                F.CopyTo(Path.Combine(aTarget.FullName, F.Name), true);
            });
        }

        public async Task CreateStructure(string aSelectBackupDir, BackupsController.PlayfieldGlobalStructureInfo aStructure)
        {
            var NewID = await Request_NewEntityId();

            var ExportInfo = new EntityExportInfo(NewID.id,
                Path.Combine(EmpyrionConfiguration.SaveGamePath, "Shared", $"{aStructure.Type}_Player_{NewID.id}"),
                false);

            var SpawnInfo = new EntitySpawnInfo()
            {
                forceEntityId = NewID.id,
                playfield = aStructure.Playfield,
                pos = aStructure.Pos,
                rot = aStructure.Rot,
                name = aStructure.Name,
                type = (byte)Array.IndexOf(new[] { "Undef", "", "BA", "CV", "SV", "HV", "", "AstVoxel" }, aStructure.Type), // Entity.GetFromEntityType 'Kommentare der Devs: Set this Undef = 0, BA = 2, CV = 3, SV = 4, HV = 5, AstVoxel = 7
                entityTypeName = "", // 'Kommentare der Devs:  ...or set this to f.e. 'ZiraxMale', 'AlienCivilian1Fat', etc
                prefabName = $"{aStructure.Type}_Player",
                factionGroup = 0,
                factionId = 0, // erstmal auf "public" aStructure.Faction,
            };

            var SourceDir = Path.Combine(BackupDir, aSelectBackupDir, @"Saves\Games",
                            Path.GetFileName(EmpyrionConfiguration.SaveGamePath), "Shared", aStructure.structureName);
            var TargetDir = Path.Combine(EmpyrionConfiguration.SaveGamePath, "Shared", $"{aStructure.Type}_Player_{NewID.id}");

            Directory.CreateDirectory(Path.GetDirectoryName(TargetDir));
            CopyAll(new DirectoryInfo(SourceDir), new DirectoryInfo(TargetDir));

            try { await Request_Load_Playfield(new PlayfieldLoad(20, aStructure.Playfield, 0)); }
            catch { }  // Playfield already loaded

            await Request_Entity_Spawn(SpawnInfo);
            await Request_Structure_Touch(NewID); // Sonst wird die Struktur sofort wieder gelöscht !!!
        }

        public void BackupState(bool aRunning)
        {
            SysteminfoManager.Value.CurrentSysteminfo.online =
                SysteminfoManager.Value.SetState(SysteminfoManager.Value.CurrentSysteminfo.online, "b", aRunning);
        }

        public void FullBackup(string aCurrentBackupDir)
        {
            BackupState(true);

            SavegameBackup(aCurrentBackupDir);
            ScenarioBackup(aCurrentBackupDir);
            ModsBackup(aCurrentBackupDir);
            EGSMainFilesBackup(aCurrentBackupDir);

            BackupState(false);
        }

        public void SavegameBackup(string aCurrentBackupDir)
        {
            BackupState(true);

            CopyAll(new DirectoryInfo(Path.Combine(EmpyrionConfiguration.ProgramPath, "Saves", "Games")),
                new DirectoryInfo(Path.Combine(aCurrentBackupDir, "Saves", "Games")));

            CopyAll(new DirectoryInfo(Path.Combine(EmpyrionConfiguration.ProgramPath, "Saves", "Cache")),
                new DirectoryInfo(Path.Combine(aCurrentBackupDir, "Saves", "Cache")));

            CopyAll(new DirectoryInfo(Path.Combine(EmpyrionConfiguration.ProgramPath, "Saves", "Blueprints")),
                new DirectoryInfo(Path.Combine(aCurrentBackupDir, "Saves", "Blueprints")));

            Directory.EnumerateFiles(Path.Combine(EmpyrionConfiguration.ProgramPath, "Saves"))
                .AsParallel()
                .ForEach(F => File.Copy(F, Path.Combine(aCurrentBackupDir, "Saves", Path.GetFileName(F))));

            BackupState(false);
        }

        public void StructureBackup(string aCurrentBackupDir)
        {
            BackupState(true);

            CopyAll(new DirectoryInfo(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Shared")),
                new DirectoryInfo(Path.Combine(aCurrentBackupDir,
                "Saves", "Games", EmpyrionConfiguration.DedicatedYaml.SaveGameName, "Shared")));

            BackupState(false);
        }

        public void ScenarioBackup(string aCurrentBackupDir)
        {
            BackupState(true);

            CopyAll(new DirectoryInfo(Path.Combine(EmpyrionConfiguration.ProgramPath, "Content", "Scenarios", EmpyrionConfiguration.DedicatedYaml.CustomScenarioName)),
                new DirectoryInfo(Path.Combine(aCurrentBackupDir, EmpyrionConfiguration.DedicatedYaml.CustomScenarioName)));

            BackupState(false);
        }

        public void ModsBackup(string aCurrentBackupDir)
        {
            BackupState(true);

            CopyAll(new DirectoryInfo(Path.Combine(EmpyrionConfiguration.ProgramPath, "Content", "Mods")),
                new DirectoryInfo(Path.Combine(aCurrentBackupDir, "Mods")));

            BackupState(false);
        }

        public void EGSMainFilesBackup(string aCurrentBackupDir)
        {
            BackupState(true);

            var MainBackupFiles = new[] { ".yaml", ".cmd", ".txt" };

            Directory.EnumerateFiles(Path.Combine(EmpyrionConfiguration.ProgramPath))
                .Where(F => MainBackupFiles.Contains(Path.GetExtension(F).ToLower()))
                .AsParallel()
                .ForEach(F => File.Copy(F, Path.Combine(aCurrentBackupDir, Path.GetFileName(F))));

            BackupState(false);
        }

        public void DeleteOldBackups(int aDays)
        {
            Directory.EnumerateDirectories(BackupDir)
                .Where(D => D.Length > 8 && !D.Contains("#"))
                .Select(D => new Tuple<string, DateTime>(D, DateTime.TryParseExact(D.Substring(0, 8), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime Result) ? Result : DateTime.MaxValue))
                .Where(T => T.Item2 < (DateTime.Today - new TimeSpan(aDays, 0, 0, 0)))
                .AsParallel()
                .ForAll(T => Directory.Delete(T.Item1, true));
        }
    }

    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class BackupsController : ControllerBase
    {
        public class BackupData
        {
            public string backup;
        }


        public BackupManager BackupManager { get; }

        public BackupsController()
        {
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
            if (!Directory.Exists(BackupManager.BackupDir)) return Ok();
            return Ok(Directory.EnumerateDirectories(BackupManager.BackupDir).Where(D => D.Contains("Backup")).OrderByDescending(D => D).Select(D => Path.GetFileName(D)));
        }

        [HttpPost("ReadStructures")]
        public IActionResult ReadStructures([FromBody]BackupData aSelectBackupDir)
        {
            var StructDir = Path.Combine(BackupManager.BackupDir, aSelectBackupDir.backup, @"Saves\Games", Path.GetFileName(EmpyrionConfiguration.SaveGamePath), "Shared");
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

        public class CreateStructureData : BackupData
        {
            public PlayfieldGlobalStructureInfo structure;
        }

        [HttpPost("CreateStructure")]
        public IActionResult CreateStructure([FromBody]CreateStructureData aData)
        {
            BackupManager.CreateStructure(aData.backup, aData.structure).Wait();
            return Ok();
        }

        public class MarkBackupData : BackupData
        {
            public string mark;
        }

        [HttpPost("MarkBackup")]
        public IActionResult MarkBackup([FromBody]MarkBackupData aData)
        {
            var NameLen = aData.backup.IndexOf(" # ");
            if (NameLen == -1) NameLen = aData.backup.Length;
            Directory.Move(
                Path.Combine(BackupManager.BackupDir, aData.backup), 
                Path.Combine(BackupManager.BackupDir, aData.backup.Substring(0, NameLen) + (string.IsNullOrEmpty(aData.mark) ? "" : " # " + aData.mark)));
            return Ok();
        }

    }
}