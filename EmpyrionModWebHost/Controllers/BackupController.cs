using System.Globalization;
using System.Collections.Concurrent;
using EgsDbTools;
using System.IO.Compression;
using System.ComponentModel;

namespace EmpyrionModWebHost.Controllers
{
    public class BackupStructureData
    {
        public List<PlayfieldGlobalStructureInfo> AlivePlayerStructures { get; set; }
        public List<PlayfieldGlobalStructureInfo> DeletedPlayerStructures { get; set; }
    }

    public class BackupManager : EmpyrionModBase, IEWAPlugin
    {
        public const string CurrentSaveGame = "### Current Savegame ###";
        public const string PreBackupDirectoryName = "PreBackup";
        public readonly static string[] EntityTypes = new[] { "Undef", "", "BA", "CV", "SV", "HV", "", "AstVoxel" };

    public ILogger<BackupManager> Logger { get; set; }
        public IMapper Mapper { get; }
        public ModGameAPI GameAPI { get; private set; }
        public Lazy<StructureManager> StructureManager { get; }
        public Lazy<SysteminfoManager> SysteminfoManager { get; }
        public string BackupDir { get; internal set; }
        public Queue<PlayfieldGlobalStructureInfo> SavesStructuresDat { get; set; }
        public ConcurrentDictionary<string, string> ActivePlayfields { get; set; } = new ConcurrentDictionary<string, string>();
        public ConcurrentDictionary<string, bool> LoggedError { get; set; } = new ConcurrentDictionary<string, bool>();

        public ConfigurationManager<BackupStructureData> BackupStructureDB { get; set; }

        static int copiedFiles  = 0;
        static int skippedFiles = 0;
        private int BackupStructureDataLogCounter;

        public string CurrentBackupDirectory(string aAddOn) {
            var Result = Path.Combine(BackupDir, aAddOn == PreBackupDirectoryName ? PreBackupDirectoryName : $"{DateTime.Now.ToString("yyyyMMdd HHmm")} Backup{aAddOn}");
            Directory.CreateDirectory(Result);

            return Result;
        }

        public BackupManager(ILogger<BackupManager> logger, IMapper mapper)
        {
            Logger = logger;
            Mapper = mapper;

            StructureManager  = new Lazy<StructureManager> (() => Program.GetManager<StructureManager>());
            SysteminfoManager = new Lazy<SysteminfoManager>(() => Program.GetManager<SysteminfoManager>());

            BackupDir = Path.Combine(EmpyrionConfiguration.ProgramPath, "Saves", Program.AppSettings.BackupDirectory ?? "Backup");

            BackupStructureDB = new ConfigurationManager<BackupStructureData>()
            {
                ConfigFilename = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "EWA", "DB", "BackupStructureDB.json")
            };
            BackupStructureDB.Load();
        }

        private void BackupStructureData()
        {
            if (ActivePlayfields.IsEmpty || !SysteminfoManager.Value.EGSIsRunning) return;

            bool displayInfo = false;
            if (SavesStructuresDat == null || SavesStructuresDat.Count == 0)
            {
                SavesStructuresDat = new Queue<PlayfieldGlobalStructureInfo>(BackupStructureDB.Current.AlivePlayerStructures
                            .Where(S => ActivePlayfields.TryGetValue(S.Playfield, out _)));
                displayInfo = true;
            }

            int errorCounter = 0;

            while (SavesStructuresDat.TryDequeue(out var test))
            {
                try
                {
                    var structurePath = Path.Combine(EmpyrionConfiguration.SaveGamePath, "Shared", $"{test.Id}");
                    var exportDat = Path.Combine(structurePath, "Export.dat");
                    // var entsDat   = Path.Combine(structurePath, "ents.dat");   // A12 autosave: Funktioniert leider beim Restore nicht

                    if (ActivePlayfields.TryGetValue(test.Playfield, out _) && IsExportDatOutdated(exportDat)) // Funktioniert leider beim Restore nicht: && !File.Exists(entsDat))
                    {
                        if (displayInfo)
                        {
                            if(BackupStructureDataLogCounter++ % 100 == 0) Logger.LogInformation("BackupStructureData: SavesStructuresDat #{SavesStructuresDat} for ActivePlayfields #{ActivePlayfields}", SavesStructuresDat.Count, ActivePlayfields.Count);
                            else                                           Logger.LogDebug      ("BackupStructureData: SavesStructuresDat #{SavesStructuresDat} for ActivePlayfields #{ActivePlayfields}", SavesStructuresDat.Count, ActivePlayfields.Count);

                            displayInfo = false;
                        }

                        Request_Entity_Export(new EntityExportInfo()
                        {
                            id            = test.Id,
                            playfield     = test.Playfield,
                            filePath      = exportDat,
                            isForceUnload = false,
                        }).Wait(10000);

                        Thread.Sleep(Program.AppSettings.SleepBetweenEntityExportInSeconds * 1000);
                        return;
                    }
                }
                catch (TimeoutException) { }
                catch (Exception error)  {
                    if (LoggedError.TryAdd(error.Message, true)) Logger?.LogError(error, $"BackupStructureData: Request_Entity_Export[{errorCounter++}] {test.Playfield} -> {test.Id} '{test.Name}'");

                    Thread.Sleep(Program.AppSettings.SleepBetweenEntityExportInSeconds * 1000);
                }
            }
        }

        private bool IsExportDatOutdated(string exportDat)
        {
            return !File.Exists(exportDat) || (DateTime.Now - File.GetLastWriteTime(exportDat)).TotalMinutes > Program.AppSettings.ExportDatOutdatedInMinutes;
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            LogLevel = EmpyrionNetAPIDefinitions.LogLevel.Debug;

            _ = TaskTools.Intervall(Math.Max(1, Program.AppSettings.BackupStructureDataUpdateCheckInSeconds) * 1000,           BackupStructureData);
            _ = TaskTools.Intervall(Math.Max(1, Program.AppSettings.BackupStructureDataUpdateCheckInSeconds) * 1000 * 60 * 60, LoggedError.Clear);
            _ = TaskTools.Intervall(Math.Max(1, Program.AppSettings.BackupStructureDataUpdateCheckInSeconds) * 1000 * 60,      UpdateGlobalStuctureInfoData);

            Event_Playfield_Loaded   += P => ActivePlayfields.TryAdd   (P.playfield, P.playfield);
            Event_Playfield_Unloaded += P => ActivePlayfields.TryRemove(P.playfield, out _);
        }

        private void UpdateGlobalStuctureInfoData()
        {
            var currentAlivePlayerStructures = StructureManager.Value.LastGlobalStructureList.Current?.globalStructures?
                .SelectMany(PS => PS.Value
                    .Where(S => S.factionId > 0 && (S.factionGroup == (byte)Factions.Private || S.factionGroup == (byte)Factions.Faction))
                    .Select(S =>
                    {
                        var result = Mapper.Map<PlayfieldGlobalStructureInfo>(S);
                        result.Playfield     = PS.Key;
                        result.structureName = $"{S.id}";
                        result.Type          = GetEntityTypeName(S.type); // Entity.GetFromEntityType 'Kommentare der Devs: Set this Undef = 0, BA = 2, CV = 3, SV = 4, HV = 5, AstVoxel = 7
                        return result;
                    }))
                .ToList();

            if (currentAlivePlayerStructures == null) return;

            if(BackupStructureDB.Current.AlivePlayerStructures == null || BackupStructureDB.Current.AlivePlayerStructures.Count == 0)
            {
                BackupStructureDB.Current.AlivePlayerStructures   = currentAlivePlayerStructures;
                BackupStructureDB.Current.DeletedPlayerStructures = new List<PlayfieldGlobalStructureInfo>();
            }
            else
            {
                var dictList = BackupStructureDB.Current.AlivePlayerStructures.ToDictionary(S => S.Id, S => S);
                currentAlivePlayerStructures.ForEach(S => dictList.Remove(S.Id));

                BackupStructureDB.Current.AlivePlayerStructures = currentAlivePlayerStructures;
                BackupStructureDB.Current.DeletedPlayerStructures.AddRange(dictList.Values);
            }

            BackupStructureDB.Save();
        }

        private static string GetEntityTypeName(byte entityTypeId)
        {
            return EntityTypes.Length > entityTypeId && entityTypeId > 0 ? EntityTypes[entityTypeId] : "???";
        }

        public static void CopyAll(DirectoryInfo aSource, DirectoryInfo aTarget, bool onlyIfNewerOrFilesizeDiff)
        {
            aSource.GetDirectories().AsParallel().ForEach(D =>
            {
                try { aTarget.CreateSubdirectory(D.Name); } catch { }
                CopyAll(D, new DirectoryInfo(Path.Combine(aTarget.FullName, D.Name)), onlyIfNewerOrFilesizeDiff);
            });

            var targetFiles = aTarget.Exists ? new ConcurrentDictionary<string, string>(aTarget.GetFiles().ToDictionary(F => F.Name, F => F.Name)) : new ConcurrentDictionary<string, string>();

            aSource.GetFiles().AsParallel().ForEach(F => {
                Directory.CreateDirectory(aTarget.FullName);
                try {
                    var targetFilename = Path.Combine(aTarget.FullName, F.Name);
                    targetFiles.TryRemove(F.Name, out _);

                    if (onlyIfNewerOrFilesizeDiff
                        && File.Exists(targetFilename)
                        && File.GetLastWriteTimeUtc(targetFilename) == F.LastWriteTimeUtc
                        && new FileInfo(targetFilename).Length == F.Length
                        )
                    {
                        Interlocked.Increment(ref skippedFiles);
                        return;
                    }

                    F.CopyTo(targetFilename, true);
                    Interlocked.Increment(ref copiedFiles);
                }
                catch { }
            });

            targetFiles.AsParallel().ForAll(F => {
                try{ File.Delete(Path.Combine(aTarget.FullName, F.Key)); }
                catch { }
            });

        }

        public async Task CreateStructure(string aSelectBackupDir, PlayfieldGlobalStructureInfo aStructure)
        {
            var NewID = await Request_NewEntityId();

            var SourceDir = Path.Combine(BackupDir,
                            aSelectBackupDir == CurrentSaveGame ? EmpyrionConfiguration.ProgramPath : aSelectBackupDir,
                            @"Saves\Games",
                            Path.GetFileName(EmpyrionConfiguration.SaveGamePath), "Shared", aStructure.structureName);

            var sourceExportDat = Path.Combine(SourceDir, "Export.dat");

            // zur Zeit funktioniert das Restaurtieren mit den Devicesettings nicht mit der vom Spiel erzeugten *.dat Datei :-( 
            //if (!File.Exists(sourceExportDat)) sourceExportDat = Path.Combine(SourceDir, "ents.dat"); // Empyrion autosave

            var TargetDir = Path.Combine(EmpyrionConfiguration.SaveGamePath, "Shared", $"{NewID.id}");

            var SpawnInfo = new EntitySpawnInfo()
            {
                forceEntityId   = NewID.id,
                playfield       = aStructure.Playfield,
                pos             = new PVector3(aStructure.Pos?.x ?? 0, aStructure.Pos?.y ?? 0, aStructure.Pos?.z ?? 0),
                rot             = new PVector3(aStructure.Rot?.x ?? 0, aStructure.Rot?.y ?? 0, aStructure.Rot?.z ?? 0),
                name            = aStructure.Name,
                type            = (byte)Array.IndexOf(EntityTypes, aStructure.Type), // Entity.GetFromEntityType 'Kommentare der Devs: Set this Undef = 0, BA = 2, CV = 3, SV = 4, HV = 5, AstVoxel = 7
                entityTypeName  = "", // 'Kommentare der Devs:  ...or set this to f.e. 'ZiraxMale', 'AlienCivilian1Fat', etc
                //prefabName      = NewID.id.ToString(),
                factionGroup    = 0,
                factionId       = 0, // erstmal auf "public" aStructure.Faction,
                exportedEntityDat = File.Exists(sourceExportDat) ? sourceExportDat : null
            };

            Directory.CreateDirectory(Path.GetDirectoryName(TargetDir));
            CopyAll(new DirectoryInfo(SourceDir), new DirectoryInfo(TargetDir), false);

            Logger.LogInformation("SourceDir:{SourceDir} TargetDir:{TargetDir} -> {SpawnInfo}", SourceDir, TargetDir, JsonConvert.SerializeObject(SpawnInfo));

            try { await Request_Load_Playfield(new PlayfieldLoad(20, aStructure.Playfield, 0)); }
            catch { }  // Playfield already loaded

            try
            {
                await Request_Entity_Spawn(SpawnInfo);
                for (int i = 0; i < 10; i++)
                {
                    try
                    {
                        await Request_Structure_Touch(NewID); // Sonst wird die Struktur sofort wieder gelöscht !!!
                        break;
                    }
                    catch
                    {
                        await Task.Delay(5000);
                    }
                }
            }
            catch (Exception error)
            {
                Logger.LogError(error, "CreateStructure failed");
            }
        }

        public void BackupState(bool aRunning)
        {
            SysteminfoManager.Value.CurrentSysteminfo.online =
                SysteminfoManager.Value.SetState(SysteminfoManager.Value.CurrentSysteminfo.online, "b", aRunning);
        }

        public void FullBackup(string aCurrentBackupDir)
        {
            var preBackupDir = Path.Combine(Path.GetDirectoryName(aCurrentBackupDir), PreBackupDirectoryName);
            var usePreBackup = Directory.Exists(preBackupDir);

            copiedFiles = skippedFiles = 0;

            using var _ = Logger.BeginScope($"FullBackup: use PreBackup={usePreBackup}");
            Logger.LogInformation("FullBackup:start {CurrentBackupDir}: use PreBackup={usePreBackup}", aCurrentBackupDir, usePreBackup);
            BackupState(true);

            var currentBackupDir = usePreBackup ? preBackupDir : aCurrentBackupDir;

            SavegameBackup      (currentBackupDir, usePreBackup);
            ScenarioBackup      (currentBackupDir, usePreBackup);
            ModsBackup          (currentBackupDir, usePreBackup);
            EGSMainFilesBackup  (currentBackupDir, usePreBackup);

            if (usePreBackup && preBackupDir != aCurrentBackupDir)
            {
                try
                {
                    Directory.Delete(aCurrentBackupDir, true);
                    Directory.Move(preBackupDir, aCurrentBackupDir);
                    Logger.LogInformation("PreBackup {preBackupDir} to FullBackup {aCurrentBackupDir}", preBackupDir, aCurrentBackupDir);
                }
                catch (Exception error)
                {
                    Logger.LogError("PreBackup {preBackupDir} to FullBackup {aCurrentBackupDir}: {error}", preBackupDir, aCurrentBackupDir, error);
                }
            }

            BackupState(false);
            Logger.LogInformation("FullBackup:finished copied {copiedFiles} file skipped(up to date) {skippedFiles}", copiedFiles, skippedFiles);
        }

        public void BackupPlayfields(string aCurrentBackupDir, string[] playfields, bool onlyIfNewerOrFilesizeDiff)
        {
            Logger.LogInformation("BackupPlayfields:start {CurrentBackupDir} -> {PlayfieldsCount}", aCurrentBackupDir, playfields.Length);
            BackupState(true);

            playfields.AsParallel()
                .ForAll(P =>
                {
                    CopyAll(
                        new DirectoryInfo(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Playfields", P)),
                        new DirectoryInfo(Path.Combine(aCurrentBackupDir, "Saves", "Games", Path.GetFileName(EmpyrionConfiguration.SaveGamePath), "Playfields", P)), onlyIfNewerOrFilesizeDiff);

                    CopyAll(
                        new DirectoryInfo(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Templates", P)),
                        new DirectoryInfo(Path.Combine(aCurrentBackupDir, "Saves", "Games", Path.GetFileName(EmpyrionConfiguration.SaveGamePath), "Templates", P)), onlyIfNewerOrFilesizeDiff);

                    StructureManager.Value.LastGlobalStructureList.Current.globalStructures.TryGetValue(P, out var structures);
                    structures
                        .Where(S => S.factionId > 0 && (S.factionGroup == (byte)Factions.Private || S.factionGroup == (byte)Factions.Faction))
                        .AsParallel()
                        .ForAll(S => {
                            var structureName = $"{S.id}";

                            CopyAll(
                                new DirectoryInfo(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Shared", structureName)),
                                new DirectoryInfo(Path.Combine(aCurrentBackupDir, "Saves", "Games", Path.GetFileName(EmpyrionConfiguration.SaveGamePath), "Shared", structureName)), onlyIfNewerOrFilesizeDiff);

                            File.Copy(
                                Path.Combine(EmpyrionConfiguration.SaveGamePath, "Shared", structureName + ".txt"),
                                Path.Combine(aCurrentBackupDir, "Saves", "Games", Path.GetFileName(EmpyrionConfiguration.SaveGamePath), "Shared", structureName + ".txt"),
                                true);

                            Interlocked.Increment(ref copiedFiles);
                        });
                });

            CopyStructureDBToBackup(aCurrentBackupDir);

            BackupState(false);
            Logger.LogInformation("BackupPlayfields:finished");
        }

        public void SavegameBackup(string aCurrentBackupDir, bool onlyIfNewerOrFilesizeDiff)
        {
            Logger.LogInformation("SavegameBackup:start {aCurrentBackupDir}", aCurrentBackupDir);
            BackupState(true);

            CopyAll(new DirectoryInfo(Path.Combine(EmpyrionConfiguration.SaveGamePath, "..", "..", "Games")),
                new DirectoryInfo(Path.Combine(aCurrentBackupDir, "Saves", "Games")), onlyIfNewerOrFilesizeDiff);

            CopyAll(new DirectoryInfo(Path.Combine(EmpyrionConfiguration.SaveGamePath, "..", "..", "Cache")),
                new DirectoryInfo(Path.Combine(aCurrentBackupDir, "Saves", "Cache")), onlyIfNewerOrFilesizeDiff);

            CopyAll(new DirectoryInfo(Path.Combine(EmpyrionConfiguration.SaveGamePath, "..", "..", "Blueprints")),
                new DirectoryInfo(Path.Combine(aCurrentBackupDir, "Saves", "Blueprints")), onlyIfNewerOrFilesizeDiff);

            Directory.EnumerateFiles(Path.Combine(EmpyrionConfiguration.SaveGamePath, "..", ".."))
                .AsParallel()
                .ForEach(F =>
                {
                    File.Copy(F, Path.Combine(aCurrentBackupDir, "Saves", Path.GetFileName(F)), true);
                    Interlocked.Increment(ref copiedFiles);
                });

            CopyStructureDBToBackup(aCurrentBackupDir);

            BackupState(false);
            Logger.LogInformation("SavegameBackup:finished");
        }

        public void StructureBackup(string aCurrentBackupDir, bool onlyIfNewerOrFilesizeDiff)
        {
            Logger.LogInformation("StructureBackup:start {CurrentBackupDir}", aCurrentBackupDir);
            BackupState(true);

            CopyAll(new DirectoryInfo(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Shared")),
                new DirectoryInfo(Path.Combine(aCurrentBackupDir,
                "Saves", "Games", EmpyrionConfiguration.DedicatedYaml.SaveGameName, "Shared")), onlyIfNewerOrFilesizeDiff);

            CopyStructureDBToBackup(aCurrentBackupDir);

            BackupState(false);
            Logger.LogInformation("StructureBackup:finished");
        }

        public void PlayersBackup(string aCurrentBackupDir, bool onlyIfNewerOrFilesizeDiff)
        {
            Logger.LogInformation("PlayerBackup:start {CurrentBackupDir}", aCurrentBackupDir);
            BackupState(true);

            CopyAll(new DirectoryInfo(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Players")),
                new DirectoryInfo(Path.Combine(aCurrentBackupDir,
                "Saves", "Games", EmpyrionConfiguration.DedicatedYaml.SaveGameName, "Players")), onlyIfNewerOrFilesizeDiff);

            Directory.CreateDirectory(Path.Combine(aCurrentBackupDir, "Saves", "Games", EmpyrionConfiguration.DedicatedYaml.SaveGameName, @"Mods\EWA\DB"));
            File.Copy(Path.Combine(EmpyrionConfiguration.SaveGamePath, @"Mods\EWA\DB\Players.db"), Path.Combine(aCurrentBackupDir, "Saves", "Games", EmpyrionConfiguration.DedicatedYaml.SaveGameName, @"Mods\EWA\DB\Players.db"), true);

            BackupState(false);
            Logger.LogInformation("PlayerBackup:finished");
        }

        private void CopyStructureDBToBackup(string aCurrentBackupDir)
        {
            Logger.LogInformation("CopyStructureDBToBackup:start {CurrentBackupDir}", aCurrentBackupDir);
            try
            {
                File.Copy(BackupStructureDB.ConfigFilename,
                    Path.Combine(aCurrentBackupDir, "Saves", "Games", EmpyrionConfiguration.DedicatedYaml.SaveGameName, "Shared", Path.GetFileName(BackupStructureDB.ConfigFilename)), true);

                Interlocked.Increment(ref copiedFiles);
            }
            catch (Exception error)
            {
                Logger.LogError(error, "CopyStructureDBToBackup:{0}", BackupStructureDB.ConfigFilename);
            }
            Logger.LogInformation("CopyStructureDBToBackup:finished");
        }

        public void ScenarioBackup(string aCurrentBackupDir, bool onlyIfNewerOrFilesizeDiff)
        {
            Logger.LogInformation("ScenarioBackup:start {CurrentBackupDir}", aCurrentBackupDir);
            BackupState(true);

            CopyAll(new DirectoryInfo(Path.Combine(EmpyrionConfiguration.ProgramPath, "Content", "Scenarios", EmpyrionConfiguration.DedicatedYaml.CustomScenarioName)),
                new DirectoryInfo(Path.Combine(aCurrentBackupDir, EmpyrionConfiguration.DedicatedYaml.CustomScenarioName)), onlyIfNewerOrFilesizeDiff);

            BackupState(false);
            Logger.LogInformation("ScenarioBackup:finished");
        }

        public void ModsBackup(string aCurrentBackupDir, bool onlyIfNewerOrFilesizeDiff)
        {
            Logger.LogInformation("ModsBackup:start {CurrentBackupDir}", aCurrentBackupDir);
            BackupState(true);

            CopyAll(new DirectoryInfo(Path.Combine(EmpyrionConfiguration.ProgramPath, "Content", "Mods")),
                new DirectoryInfo(Path.Combine(aCurrentBackupDir, "Mods")), onlyIfNewerOrFilesizeDiff);

            BackupState(false);
            Logger.LogInformation("ModsBackup:finished");
        }

        public void EGSMainFilesBackup(string aCurrentBackupDir, bool onlyIfNewerOrFilesizeDiff)
        {
            Logger.LogInformation("EGSMainFilesBackup:start {CurrentBackupDir}", aCurrentBackupDir);
            BackupState(true);

            var MainBackupFiles = new[] { ".yaml", ".cmd", ".txt" };

            Directory.EnumerateFiles(Path.Combine(EmpyrionConfiguration.ProgramPath))
                .Where(F => MainBackupFiles.Contains(Path.GetExtension(F).ToLower()))
                .AsParallel()
                .ForEach(F =>
                {
                    File.Copy(F, Path.Combine(aCurrentBackupDir, Path.GetFileName(F)), true);
                    Interlocked.Increment(ref copiedFiles);
                });

            BackupState(false);
            Logger.LogInformation("EGSMainFilesBackup:finished");
        }

        public void SetNormalAttributes(string path)
        {
            try
            {
                Directory.EnumerateFiles(path)
                    .AsParallel()
                    .ForEach(F => File.SetAttributes(F, FileAttributes.Normal));

                Directory.EnumerateDirectories(path)
                    .AsParallel()
                    .ForEach(F => SetNormalAttributes(F));
            }
            catch { }
        }

        public void DeleteOldBackups(int aDays)
        {
            Directory.EnumerateDirectories(BackupDir)
                .Select(D => new { FullPath = D, BackupName = Path.GetFileName(D) })
                .Where(D => D.BackupName.Length > 8 && !D.BackupName.Contains("#"))
                .Select(D => new { D.FullPath, BackupDate = DateTime.TryParseExact(D.BackupName.Substring(0, 8), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime Result) ? Result : DateTime.MaxValue })
                .Where(T => T.BackupDate < (DateTime.Today - new TimeSpan(aDays, 0, 0, 0)))
                .AsParallel()
                .ForAll(T => { SetNormalAttributes(T.FullPath); Directory.Delete(T.FullPath, true); });
        }

        public void ZipBackup(string backup)
        {
            File.Delete(Path.Combine(BackupDir, backup + ".zip"));
            ZipFile.CreateFromDirectory(Path.Combine(BackupDir, backup), Path.Combine(BackupDir, backup + ".zip"), CompressionLevel.SmallestSize, false);
        }

        public void RestorePlayfield(string aBackup, string aPlayfield)
        {
            RestoreCopy(aBackup, Path.Combine("Saves", "Cache", 
                EmpyrionConfiguration.DedicatedYaml.SaveGameName, "Playfields", aPlayfield));

            RestoreCopy(aBackup, Path.Combine("Saves", "Games",
                EmpyrionConfiguration.DedicatedYaml.SaveGameName, "Playfields", aPlayfield));

            RestoreCopy(aBackup, Path.Combine("Saves", "Games",
                EmpyrionConfiguration.DedicatedYaml.SaveGameName, "Templates", aPlayfield));
        }

        private void RestoreCopy(string aBackup, string aPath)
        {
            var SourceDir = Path.Combine(BackupDir, aBackup, aPath);
            var TargetDir = Path.Combine(EmpyrionConfiguration.ProgramPath, aPath);

            if(Directory.Exists(TargetDir)) Directory.Delete(TargetDir, true);
            if(Directory.Exists(SourceDir)) CopyAll(new DirectoryInfo(SourceDir), new DirectoryInfo(TargetDir), false);
        }
    }

    [Authorize(Roles = nameof(Role.Moderator))]
    [ApiController]
    [Route("[controller]")]
    public class BackupsController : ControllerBase
    {
        public class BackupData
        {
            public string backup { get; set; }
        }

        private static readonly object CreateStructureLock = new object();

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
        public PlayfieldGlobalStructureInfo[] ReadStructures([FromBody]BackupData aSelectBackupDir)
        {
            return aSelectBackupDir.backup == BackupManager.CurrentSaveGame
                ? DeletedStructuresFromCurrentSaveGame()
                : ReadStructuresFromDirectory(aSelectBackupDir.backup);
        }

        private PlayfieldGlobalStructureInfo[] ReadStructuresFromDirectory(string aSelectBackupDir)
        {
            var StructDir = Path.Combine(BackupManager.BackupDir,
                aSelectBackupDir == BackupManager.CurrentSaveGame ? EmpyrionConfiguration.ProgramPath : aSelectBackupDir,
                @"Saves\Games", Path.GetFileName(EmpyrionConfiguration.SaveGamePath), "Shared");

            var backupStructureDB = new ConfigurationManager<BackupStructureData>()
            {
                ConfigFilename = Path.Combine(StructDir, "BackupStructureDB.json")
            };
            backupStructureDB.Load();

            var result = new List<PlayfieldGlobalStructureInfo>();
            result.AddRange(backupStructureDB.Current?.AlivePlayerStructures   ?? new List<PlayfieldGlobalStructureInfo>());
            result.AddRange(backupStructureDB.Current?.DeletedPlayerStructures ?? new List<PlayfieldGlobalStructureInfo>());
            return result.ToArray();
        }

        private PlayfieldGlobalStructureInfo[] DeletedStructuresFromCurrentSaveGame()
        {
            var result = new List<PlayfieldGlobalStructureInfo>();
            result.AddRange(BackupManager.BackupStructureDB.Current?.DeletedPlayerStructures ?? new List<PlayfieldGlobalStructureInfo>());
            return result.ToArray();
        }

        [HttpPost("ReadPlayers")]
        public ActionResult<Player[]> ReadPlayers([FromBody]BackupData aSelectBackupDir)
        {
            var PlayersDBFilename = Path.Combine(
                BackupManager.BackupDir, aSelectBackupDir.backup, 
                @"Saves\Games", Path.GetFileName(EmpyrionConfiguration.SaveGamePath), @"Mods\EWA\DB\Players.db");

            if (System.IO.File.Exists(PlayersDBFilename))
            {
                using var DB = new PlayerContext(PlayersDBFilename);
                return DB.Players.ToArray();
            }

            return null;
        }

        public class RestorePlayerData : BackupData
        {
            public string steamId { get; set; }
        }

        [HttpPost("RestorePlayer")]
        public IActionResult RestorePlayer([FromBody]RestorePlayerData aSelect)
        {
            var PlayersSourcePath = Path.Combine(
                BackupManager.BackupDir, aSelect.backup,
                @"Saves\Games", Path.GetFileName(EmpyrionConfiguration.SaveGamePath), "Players");

            System.IO.File.Copy(
                Path.Combine(PlayersSourcePath, aSelect.steamId + ".ply"),
                Path.Combine(EmpyrionConfiguration.SaveGamePath, "Players", aSelect.steamId + ".ply"),
                true
                );

            return Ok();
        }

        [HttpPost("ReadPlayfields")]
        public ActionResult<string[]> ReadPlayfields([FromBody]BackupData aSelectBackupDir)
        {
            var TemplatesPath = Path.Combine(
                BackupManager.BackupDir, aSelectBackupDir.backup,
                @"Saves\Games", Path.GetFileName(EmpyrionConfiguration.SaveGamePath), @"Templates");

            return Directory
                .EnumerateDirectories(TemplatesPath)
                .Select(D => Path.GetFileName(D))
                .ToArray();
        }

        [HttpPost("ReadStructuresDB")]
        public ActionResult<GlobalStructureList> ReadStructuresDB([FromBody]BackupData aSelectBackupDir)
        {
            var BackupGlobalStructureList = new ConfigurationManager<GlobalStructureList>
            {
                ConfigFilename = Path.Combine(
                BackupManager.BackupDir, aSelectBackupDir.backup,
                @"Saves\Games", Path.GetFileName(EmpyrionConfiguration.SaveGamePath), @"Mods\EWA\DB\GlobalStructureList.json")
            };
            BackupGlobalStructureList.Load();

            return BackupGlobalStructureList.Current;
        }

        public class RestorePlayfieldData : BackupData
        {
            public string playfield { get; set; }
        }

        [HttpPost("RestorePlayfield")]
        public IActionResult RestorePlayfield([FromBody]RestorePlayfieldData aSelect)
        {
            BackupManager.RestorePlayfield(aSelect.backup, aSelect.playfield);
            return Ok();
        }

        public static PlayfieldGlobalStructureInfo GenerateGlobalStructureInfo(string aInfoTxtFile)
        {
            var Info = new PlayfieldGlobalStructureInfo
            {
                structureName = Path.GetFileNameWithoutExtension(aInfoTxtFile)
            };

            try
            {
                var Lines = System.IO.File.ReadAllLines(aInfoTxtFile);
                var FirstLine = Lines.FirstOrDefault();
                var LastLine  = Lines.LastOrDefault();
                if (FirstLine == null || LastLine == null) return Info;

                var FieldNames  = FirstLine.Split(',');
                var FieldValues = LastLine.Split(',').ToList();

                var posField = Array.FindIndex(FieldNames, N => N == "pos");

                if (FieldValues.Count > 22)
                {
                    var startPos = 0;
                    for (int i = 0; i <= posField; i++) startPos = LastLine.IndexOf(',', startPos + 1);

                    var endPos = LastLine.IndexOfAny(new[] { ',', ' ' }, startPos);
                    if (LastLine[endPos] == ',')
                    {
                    LastLine = LastLine.Substring(0, endPos) + '.' + LastLine.Substring(endPos + 1);
                    endPos = LastLine.IndexOf(' ', endPos) + 1;
                    }
                    endPos = LastLine.IndexOfAny(new[] { ',', ' ' }, endPos + 1);
                    if (LastLine[endPos] == ',')
                    {
                    LastLine = LastLine.Substring(0, endPos) + '.' + LastLine.Substring(endPos + 1);
                    endPos = LastLine.IndexOf(' ', endPos) + 1;
                    }
                    endPos = LastLine.IndexOfAny(new[] { ',', ' ' }, endPos + 1);
                    if (LastLine[endPos] == ',')
                    {
                    LastLine = LastLine.Substring(0, endPos) + '.' + LastLine.Substring(endPos + 1);
                    endPos = LastLine.IndexOf(' ', endPos) + 1;
                    }
                }

                var rotField = Array.FindIndex(FieldNames, N => N == "rot");
                if (FieldValues.Count > 22)
                {
                    var startPos = 0;
                    for (int i = 0; i <= rotField; i++) startPos = LastLine.IndexOf(',', startPos + 1);

                    var endPos = LastLine.IndexOfAny(new[] { ',', ' ' }, startPos);
                    if (LastLine[endPos] == ',' && Char.IsDigit(LastLine[endPos + 1]))
                    {
                    LastLine = LastLine.Substring(0, endPos) + '.' + LastLine.Substring(endPos + 1);
                    endPos = LastLine.IndexOf(' ', endPos) + 1;
                    }
                    endPos = LastLine.IndexOfAny(new[] { ',', ' ' }, endPos + 1);
                    if (LastLine[endPos] == ',' && Char.IsDigit(LastLine[endPos + 1]))
                    {
                    LastLine = LastLine.Substring(0, endPos) + '.' + LastLine.Substring(endPos + 1);
                    endPos = LastLine.IndexOf(' ', endPos) + 1;
                    }
                    endPos = LastLine.IndexOfAny(new[] { ',', ' ' }, endPos + 1);
                    if (LastLine[endPos] == ',' && Char.IsDigit(LastLine[endPos + 1]))
                    {
                    LastLine = LastLine.Substring(0, endPos) + '.' + LastLine.Substring(endPos + 1);
                    endPos = LastLine.IndexOf(' ', endPos) + 1;
                    }
                }

                FieldValues = LastLine.Split(',').ToList();

                string      StringValue    (string N) { var pos = Array.IndexOf(FieldNames, N); return pos == -1 ? null : FieldValues[pos]; }
                int         IntValue       (string N) { var pos = Array.IndexOf(FieldNames, N); return pos == -1 ? 0 : ToIntOrZero(FieldValues[pos]); }
                bool        BoolValue      (string N) { var pos = Array.IndexOf(FieldNames, N); return pos != -1 && bool.TryParse(FieldValues[pos], out bool Result) && Result; }
                Vector3Data Vector3Value   (string N) { var pos = Array.IndexOf(FieldNames, N); return pos == -1 ? new Vector3Data() : GetVector3(FieldValues[pos]); }
                DateTime    DateTimeValue  (string N) { var pos = Array.IndexOf(FieldNames, N); return pos != -1 && DateTime.TryParseExact(FieldValues[pos], "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime Result) ? Result : DateTime.MinValue; }

                Info.Playfield  = StringValue("playfield");
                Info.Id         = IntValue("id");
                Info.Name       = StringValue("name")?.Trim('\'');
                Info.Type       = StringValue("type");
                var faction     = StringValue("faction")?.Trim();
                Info.Faction    = ToIntOrZero(faction?.Replace("]", "").Substring(faction.IndexOf(' ') + 1).Trim());

                Info.Blocks         = IntValue("blocks");
                Info.Devices        = IntValue("devices");
                Info.Touched_ticks  = IntValue("touched_ticks");
                Info.Touched_id     = IntValue("touched_id");
                Info.Saved_ticks    = IntValue("saved_ticks");

                Info.Docked     = BoolValue("docked");
                Info.Powered    = BoolValue("powered");
                Info.Core       = BoolValue("core");

                Info.Pos = Vector3Value("pos");
                Info.Rot = Vector3Value("rot");

                Info.Saved_time   = DateTimeValue("saved_time");
                Info.Touched_time = DateTimeValue("touched_time");
                Info.Touched_name = StringValue("touched_name")?.Trim('\'');
                Info.Add_info     = $"{StringValue("add_info")} {faction}";
            }
            catch (Exception)
            {
            }

            return Info;
        }

        private static Vector3Data GetVector3(string aValue)
        {
            var d = aValue.Split(' ');
            return new Vector3Data() { x = ToFloatOrZero(d[0]), y = ToFloatOrZero(d[1]), z = ToFloatOrZero(d[2])};
        }

        private static int ToIntOrZero(string aValue)
        {
            return (int.TryParse(aValue?.TrimStart('0'), out int Result) ? Result : 0);
        }

        private static float ToFloatOrZero(string aValue)
        {
            return (float.TryParse(aValue, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out float Result) ? Result : 0);
        }

        public class CreateStructureData : BackupData
        {
            public PlayfieldGlobalStructureInfo structure { get; set; }
        }

        [HttpPost("CreateStructure")]
        public IActionResult CreateStructure([FromBody]CreateStructureData aData)
        {
            lock (CreateStructureLock) BackupManager.CreateStructure(aData.backup, aData.structure).Wait();
            return Ok();
        }

        public class MarkBackupData : BackupData
        {
            public string mark { get; set; }
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

        [HttpPost("ZipBackup")]
        public IActionResult ZipBackup([FromBody] MarkBackupData aData)
        {
            new Thread(() => BackupManager.ZipBackup(aData.backup)){ IsBackground = true }.Start();

            return Ok();
        }
    }
}