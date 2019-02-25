using System;
using System.Linq;
using Eleon.Modding;
using EmpyrionNetAPIAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using EmpyrionModWebHost.Services;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.IO.Compression;
using EmpyrionModWebHost.Extensions;
using EmpyrionModWebHost.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

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
        public IHubContext<PlayfieldHub> PlayfieldHub { get; internal set; }
        public ModGameAPI GameAPI { get; private set; }

        public PlayfieldManager(IHubContext<PlayfieldHub> aPlayfieldHub)
        {
            PlayfieldHub = aPlayfieldHub;
        }

        public PlayfieldInfo[] Playfields { get; set; }
        public FileSystemWatcher PlayfieldsWatcher { get; private set; }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            LogLevel = EmpyrionNetAPIDefinitions.LogLevel.Debug;

            ReadPlayfields();

            PlayfieldsWatcher = new FileSystemWatcher(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Templates"));
            PlayfieldsWatcher.Created += (S, A) => ReadPlayfields();
            PlayfieldsWatcher.Deleted += (S, A) => ReadPlayfields();
            PlayfieldsWatcher.EnableRaisingEvents = true;
        }

        public void ReadPlayfields()
        {
            Playfields = Directory
                .EnumerateDirectories(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Templates"))
                .Select(D => ReadPlayfield(D))
                .Where(D => D != null)
                .ToArray();

            PlayfieldHub?.Clients?.All.SendAsync("Update", "").Wait();
        }

        public PlayfieldInfo ReadPlayfield(string aFilename)
        {
            var PlayfieldYaml = Path.Combine(aFilename, "playfield.yaml");
            if (!File.Exists(PlayfieldYaml)) return null;

            var Result = new PlayfieldInfo() { name = Path.GetFileName(aFilename) };
            var YamlLines = File.ReadAllLines(PlayfieldYaml).Select(L => L.Trim());

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

        internal void Wipe(IEnumerable<string> aPlayfields, string aWipeType)
        {
            aPlayfields.ForEach(P => Request_ConsoleCommand(new PString($"wipe '{P}' {aWipeType}")).Wait());
        }
    }

    [ApiController]
    [Authorize(Roles = nameof(Role.GameMaster))]
    [Route("[controller]")]
    public class PlayfieldController : ControllerBase
    {
        public PlayfieldManager PlayfieldManager { get; }
        public string MapsPath { get; set; } = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "Maps");
        public ILogger<PlayfieldController> Logger { get; set; }

        public PlayfieldController(ILogger<PlayfieldController> aLogger)
        {
            Logger = aLogger;
            PlayfieldManager = Program.GetManager<PlayfieldManager>();
        }

        [HttpGet("Sectors")]
        public ActionResult<string> Sectors()
        {
            return System.IO.File.ReadAllText(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Sectors", "sectors.yaml"));
        }

        [HttpGet("Playfields")]
        public ActionResult<PlayfieldInfo[]> Playfields()
        {
            if (PlayfieldManager.Playfields == null) PlayfieldManager.ReadPlayfields();

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
                    return NotFound();
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
            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                client.Headers.Add("content-type", "application/json");
                Stream data = client.OpenRead("http://hubblesite.org/api/v3/news_release/last");
                using (StreamReader messageReader = new StreamReader(data))
                {
                    dynamic Content = JsonConvert.DeserializeObject(messageReader.ReadToEnd());
                    using (var clientImg = new System.Net.WebClient())
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(aPlayfieldMap));
                        clientImg.DownloadFile(new Uri(Content.keystone_image_2x.ToString()), aPlayfieldMap);
                    }
                }
            }
        }

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
            Directory.Delete(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Playfields", Playfield), true);
            return Ok();
        }
    }

}