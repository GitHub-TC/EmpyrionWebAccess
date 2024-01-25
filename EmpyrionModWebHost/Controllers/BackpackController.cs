﻿namespace EmpyrionModWebHost.Controllers
{
    [Authorize]
    public class BackpackHub : Hub
    {
    }

    public class BackpackManager : EmpyrionModBase, IEWAPlugin, IDatabaseConnect
    {
        public ModGameAPI GameAPI { get; private set; }
        public IHubContext<BackpackHub> BackpackHub { get; internal set; }

        public BackpackManager(IHubContext<BackpackHub> aBackpackHub)
        {
            BackpackHub = aBackpackHub;
        }

        public void CreateAndUpdateDatabase()
        {
            using var DB = new BackpackContext();

            DB.Database.Migrate();
            DB.Database.EnsureCreated();
            DB.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        }

        public void DeleteOldBackpacks(int aDays)
        {
            using var DB = new BackpackContext();

            var DelTime = DateTime.Now - new TimeSpan(aDays, 0, 0, 0);

            DB.Backpacks.RemoveRange(DB.Backpacks.Where(B => B.Timestamp < DelTime && B.Timestamp != DateTime.MinValue));

            DB.SaveChanges();
            DB.Database.ExecuteSqlRaw("VACUUM;");
        }

        public void UpdateBackpack(string aPlayerSteamId, ItemStack[] aToolbar, ItemStack[] aBag)
        {
            Backpack backpack = TaskTools.Retry(() => backpack = ExecUpdate(aPlayerSteamId, aToolbar, aBag));
            if(backpack != null) BackpackHub?.Clients.All.SendAsync("UpdateBackpack", JsonConvert.SerializeObject(backpack)).Wait();
        }

        private bool IsEqual(ItemStack[] aLeft, ItemStack[] aRight)
        {
            if (aLeft == null && aRight != null) return false;
            if (aLeft != null && aRight == null) return false;
            if (aLeft == null && aRight == null) return true;
            if (aLeft.Length != aRight.Length) return false;

            for (int i = aLeft.Length - 1; i >= 0; i--)
            {
                if (aLeft[i].id    != aRight[i].id   ) return false;
                if (aLeft[i].count != aRight[i].count) return false;
            }

            return true;
        }

        private Backpack ExecUpdate(string aPlayerSteamId, ItemStack[] aToolbar, ItemStack[] aBag)
        {
            using var DB = new BackpackContext();

            var Backpack = DB.Backpacks
                    .OrderByDescending(B => B.Timestamp)
                    .FirstOrDefault(B => B.Id == aPlayerSteamId);
            var IsNewBackpack = Backpack == null || (DateTime.Now - Backpack.Timestamp).TotalMinutes >= 1;
            var ToolbarContent = JsonConvert.SerializeObject(aToolbar);
            var BagContent = JsonConvert.SerializeObject(aBag);

            if (Backpack != null)
            {
                if (IsEqual(JsonConvert.DeserializeObject<ItemStack[]>(Backpack.ToolbarContent), aToolbar) &&
                    IsEqual(JsonConvert.DeserializeObject<ItemStack[]>(Backpack.BagContent), aBag)) return null;
            }

            if (IsNewBackpack)
            {
                Backpack = new Backpack()
                {
                    Id = aPlayerSteamId,
                    Timestamp = DateTime.Now,
                    ToolbarContent = ToolbarContent,
                    BagContent = BagContent
                };
                DB.Backpacks.Add(Backpack);
            }

            DB.SaveChanges();

            Backpack = DB.Backpacks.FirstOrDefault(B => B.Id == aPlayerSteamId && B.Timestamp == DateTime.MinValue);
            IsNewBackpack = Backpack == null;
            if (IsNewBackpack) Backpack = new Backpack()
            {
                Id = aPlayerSteamId,
                Timestamp = DateTime.MinValue,
            };

            Backpack.ToolbarContent = ToolbarContent;
            Backpack.BagContent = BagContent;
            if (IsNewBackpack) DB.Backpacks.Add(Backpack);

            var count = DB.SaveChanges();

            return Backpack;
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            LogLevel = EmpyrionNetAPIDefinitions.LogLevel.Debug;

            Event_Player_Info += PlayerManager_Event_Player_Info;
        }

        private void PlayerManager_Event_Player_Info(PlayerInfo aPlayerInfo)
        {
            if (aPlayerInfo.toolbar?.Length == 0 && aPlayerInfo.bag?.Length == 0)
            {
                BackpackHub?.Clients.All.SendAsync("UpdateBackpack", JsonConvert.SerializeObject(
                    new Backpack() {
                        Id = aPlayerInfo.steamId,
                        ToolbarContent = "[]",
                        BagContent     = "[]",
                    })).Wait();
                return;
            }

            UpdateBackpack(aPlayerInfo.steamId, aPlayerInfo.toolbar, aPlayerInfo.bag);
        }

        public void SetPlayerInventory(BackpackModel aSet)
        {
            using var DB = new PlayerContext();

            var player = DB.Players.FirstOrDefault(P => P.SteamId == aSet.SteamId);
            if (player == null) return;

            var Items = new Inventory(player.EntityId, aSet.Toolbar, aSet.Bag);

            Request_Player_SetInventory(Items).GetAwaiter().GetResult();
        }


    }

    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class BackpacksController : ControllerBase
    {
        private readonly BackpackContext _db;

        public BackpackManager BackpackManager { get; }
        public IMapper Mapper { get; }

        public static IEdmModel GetEdmModel()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<Backpack>("Backpacks");
            return builder.GetEdmModel();
        }

        public BackpacksController(IMapper mapper, BackpackContext context)
        {
            Mapper = mapper;
            _db = context;
            BackpackManager = Program.GetManager<BackpackManager>();
        }

        [Authorize(Roles = nameof(Role.GameMaster))]
        [HttpPost("AddItem")]
        public async Task<IActionResult> AddItem([FromBody]IdItemStackDTO idItemStackDTO)
        {
            try
            {
                var idItemStack = Mapper.Map<IdItemStack>(idItemStackDTO);
                await BackpackManager.Request_Player_AddItem(idItemStack);
                TaskTools.Delay(5, () => BackpackManager.Request_Player_Info(new Id(idItemStack.id)).Wait());
                return Ok();
            }
            catch (Exception Error)
            {
                return NotFound(Error.Message);
            }
        }

        [HttpGet("CurrentBackpack/{key}")]
        public IActionResult CurrentBackpack(string key)
        {
            return Ok(_db.Backpacks.FirstOrDefault(B => B.Id == key && B.Timestamp == DateTime.MinValue));
        }

        [Authorize(Roles = nameof(Role.GameMaster))]
        [HttpGet("Backpacks/{key}")]
        public IActionResult Backpacks(string key)
        {
            return Ok(_db.Backpacks.Where(B => B.Id == key).OrderByDescending(B => B.Timestamp));
        }

        [Authorize(Roles = nameof(Role.GameMaster))]
        [HttpPost("SetBackpack")]
        public IActionResult SetBackpack([FromBody]BackpackModelDTO aInventory)
        {
            BackpackManager.SetPlayerInventory(Mapper.Map<BackpackModel>(aInventory));
            return Ok();
        }

    }
}