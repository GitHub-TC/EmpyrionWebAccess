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

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            LogLevel = EmpyrionNetAPIDefinitions.LogLevel.Debug;

            ReadPlayfields();
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

            Result.isPlanet = GetYamlValue(YamlLines, "PlayfieldType") == "Planet";
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
    }

    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class PlayfieldController : ControllerBase
    {
        public PlayfieldManager PlayfieldManager { get; }
        public string MapsPath { get; set; } = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "Maps");

        public PlayfieldController()
        {
            PlayfieldManager = Program.GetManager<PlayfieldManager>();
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

            var PlayfieldMap = Path.Combine(
                    EmpyrionConfiguration.SaveGameModPath,
                    "Maps",
                    aPlayfieldname,
                    "map.png");

            if (!System.IO.File.Exists(PlayfieldMap)) return NotFound();

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
        public IActionResult Wipe([FromQuery]string Playfield, [FromQuery]string WipeType)
        {
            PlayfieldManager.Request_ConsoleCommand(new PString($"wipe '{Playfield}' {WipeType}")).Wait();
            return Ok();
        }

        [HttpGet("ResetPlayfield")]
        public IActionResult ResetPlayfield([FromQuery]string Playfield)
        {
            Directory.Delete(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Playfields", Playfield), true);
            return Ok();
        }
    }

}