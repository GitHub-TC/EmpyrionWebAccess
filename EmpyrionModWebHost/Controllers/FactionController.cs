using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionModWebHost.Models;
using EmpyrionNetAPIAccess;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.Edm;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;

namespace EmpyrionModWebHost.Controllers
{
    public enum Factions
    {
        Faction = 0,
        Private = 1,
    }

    [Authorize]
    public class FactionHub : Hub
    {
    }

    public class FactionManager : EmpyrionModBase, IEWAPlugin, IDatabaseConnect
    {
        public IHubContext<FactionHub> FactionHub { get; internal set; }
        public ModGameAPI GameAPI { get; private set; }

        public FactionManager(IHubContext<FactionHub> aFactionHub)
        {
            FactionHub = aFactionHub;
        }

        public void CreateAndUpdateDatabase()
        {
            using var DB = new FactionContext();

            DB.Database.Migrate();
            DB.Database.EnsureCreated();
            DB.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        }

        public void AddFactionToDB(Faction aFaction)
        {
            using (var DB = new FactionContext())
            {
                DB.Add(aFaction);
                DB.SaveChanges();
            }

            FactionHub?.Clients.All.SendAsync("Update", JsonConvert.SerializeObject(aFaction)).Wait();
        }

        public Faction GetFaction(int aFactionId)
        {
            using var DB = new FactionContext();

            return DB.Factions.FirstOrDefault(F => F.FactionId == aFactionId);
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            LogLevel = EmpyrionNetAPIDefinitions.LogLevel.Debug;

            Event_Faction_Changed += F => UpdateFactions().Wait();

            TaskTools.IntervallAsync(10000, UpdateFactions);
        }

        private async Task UpdateFactions()
        {
            var factions = await Request_Get_Factions(new Id(1));

            using var DB = new FactionContext();
            CleanupFactionDB(factions, DB);

            foreach (var faction in factions.factions)
            {

                var Faction = DB.Factions.FirstOrDefault(F => faction.factionId == 0 ? F.Abbrev == faction.abbrev : F.FactionId == faction.factionId) ?? new Faction();
                var IsNewFaction = string.IsNullOrEmpty(Faction.Abbrev);

                Faction.FactionId = faction.factionId;
                Faction.Name = faction.name;
                Faction.Origin = faction.origin;
                Faction.Abbrev = faction.abbrev;

                if (IsNewFaction) DB.Factions.Add(Faction);
            }

            if (DB.SaveChanges() > 0) FactionHub?.Clients.All.SendAsync("Update", JsonConvert.SerializeObject(DB.Factions)).Wait();
        }

        private static void CleanupFactionDB(FactionInfoList factions, FactionContext DB)
        {
            try
            {
                var dictFactions = factions.factions.ToDictionary(K => K.abbrev, K => K);

                DB.Factions
                    .Where(F => !dictFactions.ContainsKey(F.Abbrev))
                    .ToList()
                    .ForEach(F => DB.Factions.Remove(F));

                DB.SaveChanges();
            }
            catch (System.Exception Error)
            {
                System.Console.WriteLine(Error);
            }
        }
    }

    [Authorize]
    public class FactionsController : ODataController
    {
        public FactionContext DB { get; }
        public IHubContext<FactionHub> FactionHub { get; }
        public FactionManager FactionManager { get; }

        public FactionsController(FactionContext aFactionContext)
        {
            DB = aFactionContext;
            FactionManager = Program.GetManager<FactionManager>();
        }

        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(DB.Factions);
        }

        public static IEdmModel GetEdmModel()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<Faction>("Factions");
            return builder.GetEdmModel();
        }

    }
}
