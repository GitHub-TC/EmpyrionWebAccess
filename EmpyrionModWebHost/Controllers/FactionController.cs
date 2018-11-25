using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionModWebHost.Models;
using EmpyrionNetAPIAccess;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.OData.Edm;
using Newtonsoft.Json;
using System;
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
            using (var DB = new FactionContext()) DB.Database.EnsureCreated();
        }

        public void AddFactionToDB(Faction aFaction)
        {
            using(var DB = new FactionContext())
            {
                DB.Add(aFaction);
                DB.SaveChanges();
            }

            FactionHub?.Clients.All.SendAsync("Update", JsonConvert.SerializeObject(aFaction)).Wait();
        }

        public Faction GetFaction(int aFactionId)
        {
            using (var DB = new PlayerContext())
            {
                return DB.Find<Faction>(aFactionId);
            }
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            LogLevel = EmpyrionNetAPIDefinitions.LogLevel.Debug;

            Event_Faction_Changed += FactionManager_Event_Faction_Changed;
        }

        private void FactionManager_Event_Faction_Changed(FactionChangeInfo aFactionChange)
        {
            var factions = TaskWait.For(2, Request_Get_Factions(new Id(aFactionChange.factionId))).Result;
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
