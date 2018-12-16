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
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EmpyrionModWebHost.Controllers
{
    [Authorize]
    public class FactionHub : Hub
    {
        private FactionManager FactionManager { get; set; }

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
            using (var DB = new FactionContext())
            {
                DB.Database.Migrate();
                DB.Database.EnsureCreated();
            }
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
            using (var DB = new FactionContext())
            {
                return DB.Factions.FirstOrDefault(F => F.FactionId == aFactionId);
            }
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            LogLevel = EmpyrionNetAPIDefinitions.LogLevel.Debug;

            Event_Faction_Changed += FactionManager_Event_Faction_Changed;

            UpdateFactions();
        }

        private void UpdateFactions()
        {
            TaskTools.Intervall(10000, () =>
            {
                var factions = Request_Get_Factions(new Id(1)).Result.factions;

                using (var DB = new FactionContext())
                {
                    foreach (var faction in factions)
                    {
                        var Faction = DB.Find<Faction>(faction.factionId) ?? new Faction();
                        var IsNewFaction = Faction.FactionId == 0;

                        if (IsNewFaction) Faction.FactionId = faction.factionId;
                        Faction.Name   = faction.name;
                        Faction.Origin = faction.origin;
                        Faction.Abbrev = faction.abbrev;

                        if (IsNewFaction) DB.Factions.Add(Faction);
                    }

                    if(DB.SaveChanges() > 0) FactionHub?.Clients.All.SendAsync("UpdateFactions", JsonConvert.SerializeObject(DB.Factions)).Wait();
                }
            });
        }

        private void FactionManager_Event_Faction_Changed(FactionChangeInfo aFactionChange)
        {
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
