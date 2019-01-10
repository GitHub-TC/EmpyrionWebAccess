using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionModWebHost.Models;
using EmpyrionNetAPIAccess;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.Edm;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace EmpyrionModWebHost.Controllers
{

    public class HistoryBookManager : EmpyrionModBase, IEWAPlugin, IDatabaseConnect
    {

        public ModGameAPI GameAPI { get; private set; }

        public void CreateAndUpdateDatabase()
        {
            using (var DB = new HistoryBookContext())
            {
                DB.Database.Migrate();
                DB.Database.EnsureCreated();
            }
        }


        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
        }

    }

    [Authorize]
    public class HistoryBookOfStructuresController : ODataController
    {
        public HistoryBookManager HistoryBookManager { get; }
        public HistoryBookContext DB { get; }

        public static IEdmModel GetEdmModel()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<HistoryBookOfStructures>("HistoryBookOfStructures");
            return builder.GetEdmModel();
        }


        public HistoryBookOfStructuresController(HistoryBookContext aHistoryBookContext)
        {
            DB = aHistoryBookContext;
            HistoryBookManager = Program.GetManager<HistoryBookManager>();
        }

        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(DB.Structures);
        }

    }

    [Authorize]
    public class HistoryBookOfPlayersController : ODataController
    {
        public HistoryBookManager HistoryBookManager { get; }
        public HistoryBookContext DB { get; }

        public static IEdmModel GetEdmModel()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<HistoryBookOfPlayers>("HistoryBookOfPlayers");
            return builder.GetEdmModel();
        }


        public HistoryBookOfPlayersController(HistoryBookContext aHistoryBookContext)
        {
            DB = aHistoryBookContext;
            HistoryBookManager = Program.GetManager<HistoryBookManager>();
        }

        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(DB.Players);
        }

    }


}
