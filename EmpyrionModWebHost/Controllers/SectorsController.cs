using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionModWebHost.Models;
using EmpyrionNetAPIAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace EmpyrionModWebHost.Controllers
{
    public class SectorData
    {
        public List<int> Coordinates { get; set; }
        public string Color { get; set; }
        public string Icon { get; set; }
        public bool OrbitLine { get; set; }
        public string SectorMapType { get; set; }
        public string ImageTemplateDir { get; set; }
        public List<List<string>> Playfields { get; set; }
        public List<string> Allow { get; set; }
        public List<string> Deny { get; set; }
    }

    public class SectorsData
    {
        public List<SectorData> Sectors { get; set; }
    }

    public class SectorsManager : EmpyrionModBase, IEWAPlugin
    {
        public ModGameAPI GameAPI { get; private set; }

        public SectorsData SectorsData { get; set; }
        public IDictionary<int, string> Origins { get; set; }
        public FileSystemWatcher PlayfieldsWatcher { get; private set; }

        public SectorsManager()
        {
            PlayfieldsWatcher = new FileSystemWatcher(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Sectors"), "Sectors.yaml");
            PlayfieldsWatcher.Created += (S, A) => TaskTools.Delay(10, ReadSectors);
            PlayfieldsWatcher.Deleted += (S, A) => TaskTools.Delay(10, ReadSectors);
            PlayfieldsWatcher.Changed += (S, A) => TaskTools.Delay(10, ReadSectors);
            PlayfieldsWatcher.EnableRaisingEvents = true;

            TaskTools.Delay(1, ReadSectors);
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            LogLevel = EmpyrionNetAPIDefinitions.LogLevel.Debug;
        }

        private void ReadSectors()
        {
            using (var input = File.OpenText(Path.Combine(PlayfieldsWatcher.Path, "Sectors.yaml")))
            {
                SectorsData = YamlExtensions.YamlToObject<SectorsData>(input);
            }

            ReadOrigins();
        }

        private void ReadOrigins()
        {
            Origins = SectorsData.Sectors
                    .Where(S => S.Playfields != null && S.Playfields.Count > 0)
                    .Select(S => S.Playfields)
                    .Aggregate(new List<string>(), (O, SP) => {
                        SP.ForEach(P => { if (P.Count == 4 && !string.IsNullOrEmpty(P[3])) { O.Add(P[3]); } });
                        return O;
                    })
                    .Distinct(StringComparer.InvariantCultureIgnoreCase)
                    .ToDictionary(O => int.TryParse(O.Split(':')[1], out int Result) ? Result : 0, O => O.Split(':')[0]);
        }
    }

    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class SectorsController : Controller
    {
        public SectorsManager SectorsManager { get; }


        public SectorsController()
        {
            SectorsManager = Program.GetManager<SectorsManager>();
        }

        [HttpGet("Origins")]
        public IDictionary<int, string> Origins()
        {
            return SectorsManager.Origins;
        }

        [Authorize(Roles = nameof(Role.GameMaster))]
        [HttpGet("Sectors")]
        public IList<SectorData> Sectors()
        {
            return SectorsManager.SectorsData.Sectors;
        }

    }
}
