﻿using Eleon.Modding;
using EmpyrionNetAPIAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EmpyrionModWebHost.Services;
using Microsoft.AspNetCore.SignalR;
using System.IO.Compression;
using EmpyrionModWebHost.Extensions;
using EmpyrionModWebHost.Models;
using Newtonsoft.Json;
using EmpyrionNetAPITools;
using Microsoft.Build.Tasks;

namespace EmpyrionModWebHost.Controllers
{
    public class PlayfieldInfo
    {
        public string name { get; set; }
        public bool isPlanet { get; set; }
        public int size { get; set; }
    }

    [Authorize]
    public class PlayfieldHub : Hub
    {
        private PlayfieldManager PlayfieldManager { get; set; }
    }

    public class PlayfieldManager : EmpyrionModBase, IEWAPlugin
    {
        public ILogger<PlayfieldManager> Logger { get; }
        public IHubContext<PlayfieldHub> PlayfieldHub { get; }
        public Lazy<StructureManager> StructureManager { get; }
        public Lazy<SysteminfoManager> SysteminfoManager { get; }
        public Lazy<SectorsManager> SectorsManager { get; }

        public ModGameAPI GameAPI { get; private set; }

        public PlayfieldManager(ILogger<PlayfieldManager> logger, IHubContext<PlayfieldHub> aPlayfieldHub)
        {
            Logger              = logger;
            PlayfieldHub        = aPlayfieldHub;
            StructureManager    = new Lazy<StructureManager>    (() => Program.GetManager<StructureManager> ());
            SysteminfoManager   = new Lazy<SysteminfoManager>   (() => Program.GetManager<SysteminfoManager>());
            SectorsManager      = new Lazy<SectorsManager>      (() => Program.GetManager<SectorsManager>   ());
        }

        public PlayfieldInfo[] Playfields { get; set; }
        public FileSystemWatcher PlayfieldsWatcher { get; private set; }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            LogLevel = EmpyrionNetAPIDefinitions.LogLevel.Debug;

            ReadPlayfields();

            PlayfieldsWatcher = new FileSystemWatcher(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Playfields"));
            PlayfieldsWatcher.Created += (S, A) => ReadPlayfields();
            PlayfieldsWatcher.Deleted += (S, A) => ReadPlayfields();
            PlayfieldsWatcher.EnableRaisingEvents = true;
        }

        public void ReadPlayfields()
        {
            Playfields = Directory
                .EnumerateDirectories(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Playfields"))
                .Select(D => ReadPlayfield(D))
                .AsParallel()
                .Where(D => D != null)
                .OrderBy(D => D.name)
                .ToArray();

            PlayfieldHub?.Clients?.All.SendAsync("Update", "").Wait();
        }

        public PlayfieldInfo ReadPlayfield(string aFilename)
        {
            var PlayfieldYaml = Path.Combine(Path.GetDirectoryName(aFilename), ".." , "Templates", Path.GetFileName(aFilename), "playfield.yaml");
            if (!File.Exists(PlayfieldYaml)) return null;

            var Result = new PlayfieldInfo() { name = Path.GetFileName(aFilename) };
            IEnumerable<string> YamlLines = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    YamlLines = File.ReadAllLines(PlayfieldYaml).Select(L => L.Trim());
                    break;
                }
                catch
                {
                    Thread.Sleep(1000);
                }
            }
                

            Result.isPlanet = new[] { "Planet", "Moon" }.Any(P => string.Compare(GetYamlValue(YamlLines, "PlayfieldType"), P, StringComparison.CurrentCultureIgnoreCase) == 0);
            Result.size = int.TryParse(GetYamlValue(YamlLines, "PlanetSize"), out int S) ? S : (Result.isPlanet ? 3 : 0);

            return Result;
        }

        private string GetYamlValue(IEnumerable<string> aYamlLines, string aKey)
        {
            var Found = aYamlLines.FirstOrDefault(L => L.StartsWith(aKey));
            if (Found == null) return null;

            var DelimiterPos = Found.IndexOf(':');
            var CommentPos = Found.IndexOf('#');
            if (CommentPos == -1) CommentPos = Found.Length;

            return Found.Substring(DelimiterPos + 1, CommentPos - DelimiterPos - 1).Trim();
        }

        public void Wipe(IEnumerable<string> aPlayfields, string aWipeType)
        {
            var wipeAllPlayfields = aPlayfields.FirstOrDefault()?.Trim() == "*";

            if (wipeAllPlayfields) aPlayfields = Directory.EnumerateDirectories(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Playfields")).ToArray();

            if (!wipeAllPlayfields && SysteminfoManager.Value.EGSIsRunning) aPlayfields.Where(P => !string.IsNullOrEmpty(P)).ForEach(P => { if (P.StartsWith("S*")) WipeSolarSystem(aWipeType, P[2..]); else Request_ConsoleCommand(new PString($"wipe '{P}' {aWipeType}")).Wait(); });
            else
            {
                Logger.LogDebug("Wipe Playfields: {@name}", aPlayfields);

                var wipeinfo = aWipeType.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                Parallel.ForEach(aPlayfields, P =>
                {
                    if (P.StartsWith("S*"))
                    {
                        WipeSolarSystem(aWipeType, P[2..]);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(P) || !Directory.Exists(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Playfields", P))) return;

                        string[] wipePresets = null;
                        try
                        {
                            wipePresets = File.ReadAllLines(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Playfields", P, "wipeinfo.txt"))?.FirstOrDefault()?.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        }
                        catch { }

                        try
                        {
                            File.WriteAllLines(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Playfields", P, "wipeinfo.txt"), wipePresets == null ? wipeinfo : wipeinfo.Concat(wipePresets).Distinct());
                        }
                        catch { }
                    }
                });
            }
        }

        private void WipeSolarSystem(string aWipeType, string solarSystemName)
        {
            try
            {
                Logger.LogDebug("Wipe SolarSystem: {@name}", solarSystemName);

                var solarSystem = SectorsManager.Value.SectorsData?.SolarSystems?.FirstOrDefault(S => S.Name == solarSystemName);
                var wipePlayfieldOfSolarSystem = solarSystem?.Sectors?.SelectMany(S => S.Playfields.Select(P => P.Skip(1).FirstOrDefault()));
                if (wipePlayfieldOfSolarSystem?.Any() == true) Wipe(wipePlayfieldOfSolarSystem, aWipeType);
            }
            catch (Exception error)
            {
                Logger.LogError(error, "WipeSolarSystem");
            }
        }

        public void ResetPlayfield(params string[] aPlayfields)
        {
            aPlayfields.AsParallel().ForEach(P => Directory.Delete(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Playfields", P), true));
        }

        public void RecreatePlayfield(params string[] aPlayfields)
        {
            aPlayfields.AsParallel().ForEach(fullPath =>
            {
                var P = Path.GetFileName(fullPath);

                Directory.Delete(Path.Combine(EmpyrionConfiguration.SaveGamePath,       "Playfields", P), true);
                Directory.Delete(Path.Combine(EmpyrionConfiguration.SaveGamePath,       "Templates",  P), true);
                Directory.Delete(Path.Combine(EmpyrionConfiguration.SaveGameCachePath,  "Playfields", P), true);
            });
        }

        public void RecreateDefectPlayfield(params string[] aPlayfields)
        {
            var wipeAllPlayfields = aPlayfields.FirstOrDefault()?.Trim() == "*";

            if (wipeAllPlayfields) aPlayfields = Directory.EnumerateDirectories(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Templates")).ToArray();

            aPlayfields.AsParallel().ForEach(fullPath =>
            {
                var P = Path.GetFileName(fullPath);

                if (File.Exists(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Templates", P, "playfield.yaml"))) return;

                Logger.LogInformation("DefectPlayfield(or instance playfield): {playfield}", P);

                try { Directory.Delete(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Templates", P), true); } catch { }
                try { Directory.Delete(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Playfields", P), true);  } catch { }
                try { Directory.Delete(Path.Combine(EmpyrionConfiguration.SaveGameCachePath, "Playfields", P), true);  } catch { }
            });
        }
        public void ResetPlayfieldIfEmpty(string[] playfields)
        {
            playfields.AsParallel()
                .Where(NoPlayerStuffPresent)
                .ForEach(P => Directory.Delete(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Playfields", P), true));
        }

        private bool NoPlayerStuffPresent(string playfield)
        {
            return StructureManager.Value.LastGlobalStructureList.Current.globalStructures.TryGetValue(playfield, out var structures) &&
                structures.All(S => S.factionGroup != (byte)Factions.Faction && S.factionGroup != (byte)Factions.Private);
        }
    }

    [ApiController]
    [Authorize(Roles = nameof(Role.VIP))]
    [Route("[controller]")]
    public class PlayfieldController : ControllerBase
    {
        public PlayfieldManager PlayfieldManager { get; }
        public PlayerManager PlayerManager { get; }
        public SectorsManager SectorsManager { get; }
        public string MapsPath { get; set; } = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "EWA", "Maps");
        public ILogger<PlayfieldController> Logger { get; set; }
        public IUserService UserService { get; }

        public PlayfieldController(ILogger<PlayfieldController> aLogger, IUserService aUserService)
        {
            Logger           = aLogger;
            UserService      = aUserService;
            PlayfieldManager = Program.GetManager<PlayfieldManager>();
            PlayerManager    = Program.GetManager<PlayerManager>();
            SectorsManager   = Program.GetManager<SectorsManager>();
        }

        [Authorize(Roles = nameof(Role.GameMaster))]
        [HttpGet("Sectors")]
        public ActionResult<string> Sectors()
        {
            return YamlExtensions.ObjectToYaml(SectorsManager.SectorsData);
        }

        [HttpGet("Playfields")]
        public ActionResult<PlayfieldInfo[]> Playfields()
        {
            if (PlayfieldManager.Playfields == null) PlayfieldManager.ReadPlayfields();

            if (UserService.CurrentUser.Role == Role.VIP)
            {
                var CurrentPlayer = PlayerManager.CurrentPlayer;
                var Faction = CurrentPlayer?.FactionId;
                if (Faction == 0) return new PlayfieldInfo[] { };

                var FactionOnPlanets = new List<string>();
                PlayerManager.QueryPlayer(
                    PlayerDB => PlayerDB.Players.Where(P => P.FactionId == Faction), 
                    P => FactionOnPlanets.Add(P.Playfield));
                
                return PlayfieldManager.Playfields.Where(P => FactionOnPlanets.Contains(P.name)).ToArray();
            }

            return PlayfieldManager.Playfields;
        }

        [AllowAnonymous]
        [HttpGet("GetPlayfieldMap/{aPlayfieldname}")]
        public IActionResult GetPlayfieldMap(string aPlayfieldname)
        {
            if (!Directory.Exists(MapsPath)) return NotFound();
            if (PlayfieldManager.Playfields == null) PlayfieldManager.ReadPlayfields();

            var PlayfieldMap = Path.Combine(
                    EmpyrionConfiguration.SaveGameModPath,
                    "EWA",
                    "Maps",
                    aPlayfieldname,
                    "map.png");

            if (!System.IO.File.Exists(PlayfieldMap) && PlayfieldManager.Playfields == null) return NotFound();

            var CurrentPlayfield = PlayfieldManager.Playfields.FirstOrDefault(P => P.name == aPlayfieldname);

            if (!System.IO.File.Exists(PlayfieldMap) && (CurrentPlayfield == null || CurrentPlayfield.isPlanet)) return NotFound();

            if (!CurrentPlayfield.isPlanet &&
               (!System.IO.File.Exists(PlayfieldMap) || (DateTime.Now - System.IO.File.GetLastWriteTime(PlayfieldMap)).TotalDays > 1))
            {
                try { ReadFromHubbleImages(PlayfieldMap); }
                catch (Exception Error)
                {
                    Logger.LogError(Error, "LoadSpace: {0} to {1}", aPlayfieldname, PlayfieldMap);
                    PlayfieldMap = Path.Combine(EmpyrionConfiguration.ModPath, @"EWALoader\EWA\ClientApp\dist\ClientApp\empty.png");
                }
            }

            DateTimeOffset? LastModified = new DateTimeOffset(System.IO.File.GetLastWriteTime(PlayfieldMap));

            return PhysicalFile(
                PlayfieldMap,
                "image/png",
                aPlayfieldname + ".png",
                LastModified,
                new Microsoft.Net.Http.Headers.EntityTagHeaderValue("\"" + ETagGenerator.GetETag(PlayfieldMap, System.IO.File.ReadAllBytes(PlayfieldMap)) + "\""),
                true
                );
        }

        private void ReadFromHubbleImages(string aPlayfieldMap)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("content-type", "application/json");
            Stream data = client.GetStreamAsync("http://hubblesite.org/api/v3/news_release/last").Result;
            using StreamReader messageReader = new StreamReader(data);
            dynamic Content = JsonConvert.DeserializeObject(messageReader.ReadToEnd());
            using var clientImg = new HttpClient();
            Directory.CreateDirectory(Path.GetDirectoryName(aPlayfieldMap));
            System.IO.File.WriteAllBytes(aPlayfieldMap, clientImg.GetByteArrayAsync(new Uri("http:" + Content.keystone_image_2x.ToString())).Result);
        }

        [Authorize(Roles = nameof(Role.GameMaster))]
        [HttpPost("UploadMapFile")]
        [DisableRequestSizeLimit]
        public IActionResult UploadMapFile([FromQuery]string PlayfieldName)
        {
            Program.CreateTempPath();

            foreach (var file in Request.Form.Files)
            {
                try { Directory.CreateDirectory(MapsPath); } catch { }

                var TargetFile = Path.Combine(MapsPath, file.Name);
                using (var ToFile = System.IO.File.Create(TargetFile))
                {
                    file.OpenReadStream().CopyTo(ToFile);
                }

                switch (Path.GetExtension(TargetFile).ToLower())
                {
                    case ".zip": ZipFile.ExtractToDirectory(TargetFile, MapsPath, true); break;
                    case ".png":
                        try { Directory.CreateDirectory(Path.Combine(MapsPath, PlayfieldName)); } catch { }
                        System.IO.File.Copy(TargetFile, Path.Combine(MapsPath, PlayfieldName, Path.GetFileName(TargetFile)), true); break;
                }

                System.IO.File.Delete(TargetFile);
            }
            return Ok();
        }

        public class WipeInfo
        {
            public string Playfield { get; set; }
            public string WipeType { get; set; }
        }

        [HttpGet("Wipe")]
        [Authorize(Roles = nameof(Role.InGameAdmin))]
        public IActionResult Wipe([FromQuery]string Playfield, [FromQuery]string WipeType)
        {
            PlayfieldManager.Wipe(new[] { Playfield }, WipeType);
            return Ok();
        }

        [HttpGet("ResetPlayfield")]
        [Authorize(Roles = nameof(Role.InGameAdmin))]
        public IActionResult ResetPlayfield([FromQuery]string Playfield)
        {
            PlayfieldManager.ResetPlayfield(Playfield);
            return Ok();
        }

        [HttpGet("RecreatePlayfield")]
        [Authorize(Roles = nameof(Role.ServerAdmin))]
        public IActionResult RecreatePlayfield([FromQuery] string Playfield)
        {
            PlayfieldManager.RecreatePlayfield(Playfield);
            return Ok();
        }

        public class PlayfieldConsoleCommand
        {
            public string Playfield { get; set; }
            public string Command { get; set; }
        }

        [Authorize(Roles = nameof(Role.InGameAdmin))]
        [HttpPost("CallPlayfieldConsoleCommand")]
        public async Task CallPlayfieldConsoleCommand([FromBody] PlayfieldConsoleCommand aData)
        {
            var pf = await PlayfieldManager.Request_Playfield_Stats(new PString(aData.Playfield));
            await PlayfieldManager.Request_ConsoleCommand(new PString($"remoteex pf={pf.processId} {aData.Command}"));
        }

        [Authorize(Roles = nameof(Role.InGameAdmin))]
        [HttpGet("StopPlayfield")]
        public async Task StopPlayfield([FromQuery] string Playfield, [FromQuery] int Timeout)
        {
            var output = new StringBuilder();

            try
            {
                using var telnetClient = new PrimS.Telnet.Client("localhost", EmpyrionConfiguration.DedicatedYaml.Tel_Port, CancellationToken.None);
                await Task.Delay(1000);
                output.AppendLine(await telnetClient.TerminatedReadAsync("Enter password:"));
                await telnetClient.WriteLineAsync(EmpyrionConfiguration.DedicatedYaml.Tel_Pwd);
                await Task.Delay(1000);
                string nextLine = null;
                for (int trys = 20; trys >= 0; trys--)
                {
                    await Task.Delay(100);
                    nextLine = await telnetClient.TerminatedReadAsync("\n", TimeSpan.FromSeconds(1));
                    if(nextLine.Contains("Wrong password, please reenter password")) await telnetClient.WriteLineAsync(EmpyrionConfiguration.DedicatedYaml.Tel_Pwd);
                    else                                                             output.AppendLine(nextLine);

                    if (nextLine?.Contains("INFO:") == true) break;
                }

                Logger.LogDebug("StopPlayfield: telnet session {Output}", output);
                await telnetClient.WriteLineAsync($"stoppf '{Playfield}' {Timeout}");
                Logger.LogInformation("StopPlayfield: stoppf {Playfield} {Timeout}", Playfield, Timeout);
            }
            catch (Exception Error) { Logger.LogError(Error, $"stoppf \"{Playfield}\" {Timeout} -> {output}", output); }
        }



    }

}