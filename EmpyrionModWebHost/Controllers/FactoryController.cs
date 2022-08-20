using AutoMapper;
using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionModWebHost.Models;
using EmpyrionNetAPIAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EmpyrionModWebHost.Controllers
{

    public class FactoryManager : EmpyrionModBase, IEWAPlugin, IDatabaseConnect
    {

        public ModGameAPI GameAPI { get; private set; }

        public void CreateAndUpdateDatabase()
        {
            using var DB = new FactoryItemsContext();
            DB.Database.Migrate();
            DB.Database.EnsureCreated();
            DB.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        }

        public void DeleteOldFactoryItems(int aDays)
        {
            using var DB = new FactoryItemsContext();

            var DelTime = DateTime.Now - new TimeSpan(aDays, 0, 0, 0);

            DB.FactoryItems.RemoveRange(DB.FactoryItems.Where(B => B.Timestamp < DelTime));

            DB.SaveChanges();
            DB.Database.ExecuteSqlRaw("VACUUM;");
        }

        public void UpdateFactoryItems(PlayerInfo aPlayerInfo)
        {
            using var DB = new FactoryItemsContext();
            var FactoryItems = DB.FactoryItems
.OrderByDescending(B => B.Timestamp)
.FirstOrDefault(B => B.Id == aPlayerInfo.steamId);
            var IsNewBackpack = FactoryItems == null || (DateTime.Now - FactoryItems.Timestamp).TotalMinutes >= 1;
            var Content = JsonConvert.SerializeObject(aPlayerInfo.bpResourcesInFactory?.Select(I => new ItemStack(I.Key, (int)I.Value)));

            if (FactoryItems != null)
            {
                if (IsEqual(JsonConvert.DeserializeObject<ItemStack[]>(FactoryItems.Content),
                    aPlayerInfo.bpResourcesInFactory?.Select(I => new ItemStack(I.Key, (int)I.Value)).ToArray())) return;
            }

            if (IsNewBackpack)
            {
                FactoryItems = new FactoryItems()
                {
                    Id = aPlayerInfo.steamId,
                    Timestamp = DateTime.Now,
                    Content = Content,
                    InProduction = aPlayerInfo.bpInFactory,
                    Produced = aPlayerInfo.producedPrefabs?.Aggregate("", (S, B) => string.IsNullOrEmpty(S) ? B : S + "\t" + B),
                };
                DB.FactoryItems.Add(FactoryItems);
            }
            else
            {
                FactoryItems.InProduction = aPlayerInfo.bpInFactory;
                FactoryItems.Produced = aPlayerInfo.producedPrefabs?.Aggregate("", (S, B) => string.IsNullOrEmpty(S) ? B : S + "\t" + B);
            }

            DB.SaveChanges();

            FactoryItems = DB.FactoryItems.FirstOrDefault(B => B.Id == aPlayerInfo.steamId && B.Timestamp == DateTime.MinValue);
            IsNewBackpack = FactoryItems == null;
            if (IsNewBackpack) FactoryItems = new FactoryItems()
            {
                Id = aPlayerInfo.steamId,
                Timestamp = DateTime.MinValue,
            };

            FactoryItems.Content = Content;
            if (IsNewBackpack) DB.FactoryItems.Add(FactoryItems);

            var count = DB.SaveChanges();
        }

        private bool IsEqual(ItemStack[] aLeft, ItemStack[] aRight)
        {
            if (aLeft == null && aRight != null) return false;
            if (aLeft != null && aRight == null) return false;
            if (aLeft == null && aRight == null) return true;
            if (aLeft.Length != aRight.Length) return false;

            for (int i = aLeft.Length - 1; i >= 0; i--)
            {
                if (aLeft[i].id != aRight[i].id) return false;
                if (aLeft[i].count != aRight[i].count) return false;
            }

            return true;
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            LogLevel = EmpyrionNetAPIDefinitions.LogLevel.Debug;

            Event_Player_Info += UpdateFactoryItems;
        }

    }

    [ApiController]
    [Authorize(Roles = nameof(Role.Moderator))]
    [Route("[controller]")]
    public class FactoryController : ControllerBase
    {
        public FactoryItemsContext DB { get; }
        public IMapper Mapper { get; }
        public FactoryManager FactoryManager { get; }

        public FactoryController(FactoryItemsContext context, IMapper mapper)
        {
            DB = context;
            Mapper = mapper;
            FactoryManager = Program.GetManager<FactoryManager>();
        }

        [HttpGet("GetBlueprintResources/{aPlayerId}")]
        public async Task<ActionResult<BlueprintResourcesDTO>> GetBlueprintResources(int aPlayerId)
        {
            try
            {
                var onlinePlayers = await FactoryManager.Request_Player_List();
                if (!onlinePlayers.list.Contains(aPlayerId)) return NotFound($"Player {aPlayerId} not online");

                var PlayerInfo = await FactoryManager.Request_Player_Info(aPlayerId.ToId());

                var Result = new BlueprintResources()
                {
                    PlayerId = aPlayerId,
                    ItemStacks = new List<ItemStack>(),
                    ReplaceExisting = true,
                };
                PlayerInfo.bpResourcesInFactory?.ForEach(I => Result.ItemStacks.Add(new ItemStack() { id = I.Key, count = (int)I.Value }));
                return Ok(Mapper.Map<BlueprintResourcesDTO>(Result));
            }
            catch (Exception Error)
            {
                return NotFound(Error.Message);
            }
        }

        [Authorize(Roles = nameof(Role.GameMaster))]
        [HttpGet("FactoryItems/{key}")]
        public IActionResult FactoryItems(string key)
        {
            return Ok(DB.FactoryItems.Where(B => B.Id == key).OrderByDescending(B => B.Timestamp));
        }

        [AutoMap(typeof(BlueprintResources), ReverseMap = true)]
        public class BlueprintResourcesDTO
        {
            public int playerId { get; set; }
            public List<ItemStackDTO> itemStacks { get; set; }
            public bool replaceExisting { get; set; }
        }

        [HttpPost("SetBlueprintResources")]
        [Authorize(Roles = nameof(Role.Moderator))]
        public async Task<IActionResult> SetBlueprintResources([FromBody]BlueprintResourcesDTO aRessources)
        {
            try
            {
                //if (aRessources.replaceExisting)
                //{
                //    var p = await FactoryManager.Request_Player_Info(aRessources.playerId.ToId());
                //    var setRes = new List<ItemStackDTO>();
                //    aRessources.itemStacks.ForEach(I => {
                //        if (p.bpResourcesInFactory.TryGetValue(I.id, out var count))
                //        {
                //            if(count < I.count) setRes.Add(new ItemStackDTO() { id = I.id, count = I.count - (int)count });
                //        }
                //        else setRes.Add(I);
                //    });
                //    aRessources.itemStacks      = setRes;
                //    aRessources.replaceExisting = false;
                //}

                await FactoryManager.Request_Blueprint_Resources(Mapper.Map<BlueprintResourcesDTO, BlueprintResources>(aRessources));
                return Ok();
            }
            catch (Exception Error)
            {
                return NotFound(Error.Message);
            }
        }

        [HttpGet("FinishBlueprint/{aPlayerId}")]
        [Authorize(Roles = nameof(Role.GameMaster))]
        public async Task<IActionResult> FinishBlueprint(int aPlayerId)
        {
            try
            {
                await FactoryManager.Request_Blueprint_Finish(new Id(aPlayerId));
                return Ok();
            }
            catch (Exception Error)
            {
                return NotFound(Error.Message);
            }
        }
    }
}