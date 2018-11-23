using Eleon.Modding;
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

    public class FactionManager : EmpyrionModBase, IEWAPlugin
    {
        public IHubContext<FactionHub> FactionHub { get; internal set; }
        public ModGameAPI GameAPI { get; private set; }
        
        public FactionManager(IHubContext<FactionHub> aFactionHub)
        {
            FactionHub = aFactionHub;
        }

        public void AddFactionToDB(Faction aFaction)
        {
            using(var DB = new FactionContext())
            {
                DB.Add(aFaction);
                DB.SaveChanges();
            }

            FactionHub?.Clients.All.SendAsync("Send", JsonConvert.SerializeObject(aFaction)).Wait();
        }

        public Faction GetFaction(int aFactionId)
        {
            using (var DB = new PlayerContext())
            {
                DB.Database.EnsureCreated();
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
        }

    }

    [Authorize]
    public class FactionsController : ODataController
    {
        public IHubContext<FactionHub> FactionHub { get; }
        public FactionManager FactionManager { get; }

        public FactionsController(IHubContext<FactionHub> aFactionHub)
        {
            FactionHub = aFactionHub;
            FactionManager = Program.GetManager<FactionManager>();
            FactionManager.FactionHub = aFactionHub;
        }

        public static IEdmModel GetEdmModel()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<Faction>("Factions");
            return builder.GetEdmModel();
        }

    }
}
