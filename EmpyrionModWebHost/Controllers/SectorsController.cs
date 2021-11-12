using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionModWebHost.Models;
using EmpyrionNetAPIAccess;
using EmpyrionNetAPITools;
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

    public class SolarSystems
    {
        public string Name { get; set; }
        public List<int> Coordinates { get; set; }
        public string StarClass { get; set; }
        public List<SectorData> Sectors { get; set; }
    }

    public class SectorsData
    {
        public bool GalaxyMode { get; set; }
        public List<SectorData> Sectors { get; set; }
        public List<SolarSystems> SolarSystems { get; set; }
    }

    public class SectorsManager : EmpyrionModBase, IEWAPlugin
    {
        public ModGameAPI GameAPI { get; private set; }

        public SectorsData SectorsData { get; set; }
        public IDictionary<int, string> Origins { get; set; }
        public FileSystemWatcher PlayfieldsWatcher { get; private set; }

        public SectorsManager()
        {
            PlayfieldsWatcher = new FileSystemWatcher(Path.Combine(EmpyrionConfiguration.SaveGamePath, "Sectors"));
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
            SectorsData = ReadSectorFiles(PlayfieldsWatcher.Path);
            Origins     = ReadOrigins(SectorsData);
        }

        public static SectorsData ReadSectorFiles(string path)
        {
            SectorsData result;
            using (var input = File.OpenText(Path.Combine(path, "Sectors.yaml")))
            {
                result = YamlExtensions.YamlToObject<SectorsData>(input);
            }

            if (result.GalaxyMode)
            {
                Directory.GetFiles(path, "Sectors*.yaml")
                    .Where(F => !Path.GetFileName(F).Equals("Sectors.yaml", StringComparison.InvariantCultureIgnoreCase))
                    .ForEach(F =>
                    {
                        using var input = File.OpenText(F);

                        var AddSectorsData = YamlExtensions.YamlToObject<SectorsData>(input);
                        if (AddSectorsData.GalaxyMode) result.SolarSystems.AddRange(AddSectorsData.SolarSystems);
                    });
            }

            return result;
        }

        public static List<SectorData> FlattenSectors(SectorsData sectorsData)
        {
            if(sectorsData.SolarSystems == null) return sectorsData.Sectors;

            var sectors = sectorsData.Sectors?.ToList() ?? new List<SectorData>();

            sectorsData.SolarSystems.ForEach(U => { 
                U.Sectors.ForEach(S => {
                   sectors.Add(new SectorData(){
                        Coordinates         = new[]{ S.Coordinates[0] + U.Coordinates[0], S.Coordinates[1] + U.Coordinates[1], S.Coordinates[2] + U.Coordinates[2]}.ToList(),     
                        Color               = S.Color,           
                        Icon                = S.Icon,            
                        OrbitLine           = S.OrbitLine,       
                        SectorMapType       = S.SectorMapType,   
                        ImageTemplateDir    = S.ImageTemplateDir,
                        Playfields          = S.Playfields,      
                        Allow               = S.Allow,           
                        Deny                = S.Deny,            
                   }); 
                });
            });

            return sectors;
        }

        public static IDictionary<int, string> ReadOrigins(SectorsData sectorsData)
        {
            var origins = sectorsData.Sectors?
                    .Where(S => S.Playfields != null && S.Playfields.Count > 0)
                    .Select(S => S.Playfields)
                    .Aggregate(new List<string>(), (O, SP) => {
                        SP.ForEach(P => { if (P.Count == 4 && !string.IsNullOrEmpty(P[3])) { O.Add(P[3]); } });
                        return O;
                    })
                    .Distinct(StringComparer.InvariantCultureIgnoreCase)
                    .ToDictionary(O => int.TryParse(O.Split(':')[1], out int Result) ? Result : 0, O => O.Split(':')[0])
                    ?? new Dictionary<int, string>();

            if(sectorsData.SolarSystems != null){
                sectorsData.SolarSystems.SelectMany(U => 
                    U.Sectors
                    .Where(S => S.Playfields != null && S.Playfields.Count > 0)
                    .Select(S => S.Playfields)
                    .Aggregate(new List<string>(), (O, SP) => {
                        SP.ForEach(P => { if (P.Count == 4 && !string.IsNullOrEmpty(P[3])) { O.Add(P[3]); } });
                        return O;
                    })
                    .Distinct(StringComparer.InvariantCultureIgnoreCase)
                    .ToDictionary(O => int.TryParse(O.Split(':')[1], out int Result) ? Result : 0, O => O.Split(':')[0])
                )
                .ForEach(O => origins.Add(O.Key, O.Value));
            }

            return origins;
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
        public IDictionary<string, string> Origins()
        {
            return SectorsManager.Origins?.ToDictionary(E => E.Key.ToString(), E => E.Value);
        }

        [Authorize(Roles = nameof(Role.GameMaster))]
        [HttpGet("Sectors")]
        public IList<SectorData> Sectors()
        {
            return SectorsManager.FlattenSectors(SectorsManager.SectorsData);
        }

    }
}
