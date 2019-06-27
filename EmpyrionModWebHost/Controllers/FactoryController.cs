using Eleon.Modding;
using EmpyrionModWebHost.Extensions;
using EmpyrionModWebHost.Models;
using EmpyrionNetAPIAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmpyrionModWebHost.Controllers
{

    public class FactoryManager : EmpyrionModBase, IEWAPlugin, IDatabaseConnect
    {

        public ModGameAPI GameAPI { get; private set; }

        public void CreateAndUpdateDatabase()
        {
            using (var DB = new FactoryItemsContext())
            {
                DB.Database.Migrate();
                DB.Database.EnsureCreated();
                DB.Database.ExecuteSqlCommand("PRAGMA journal_mode=WAL;");
            }
        }

        public void DeleteOldBackpacks(int aDays)
        {
            using (var DB = new FactoryItemsContext())
            {
                DB.FactoryItems
                    .Where(B => B.Timestamp != DateTime.MinValue && (DateTime.Now - B.Timestamp).TotalDays > aDays)
                    .ToList()
                    .ForEach(B => DB.FactoryItems.Remove(B));
                DB.SaveChanges();
                DB.Database.ExecuteSqlCommand("VACUUM;");
            }
        }

        public void UpdateFactoryItems(PlayerInfo aPlayerInfo)
        {
            using (var DB = new FactoryItemsContext())
            {
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
                        Id              = aPlayerInfo.steamId,
                        Timestamp       = DateTime.Now,
                        Content         = Content,
                        InProduction    = aPlayerInfo.bpInFactory,
                        Produced        = aPlayerInfo.producedPrefabs?.Aggregate("", (S, B) => string.IsNullOrEmpty(S) ? B :  S + "\t" + B),
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
        public FactoryItemsContext _db { get; }
        public FactoryManager FactoryManager { get; }

        public FactoryController(FactoryItemsContext context)
        {
            _db = context;
            FactoryManager = Program.GetManager<FactoryManager>();
        }

        [HttpGet("GetBlueprintResources/{aPlayerId}")]
        public async Task<ActionResult<BlueprintResources>> GetBlueprintResources(int aPlayerId)
        {
            try
            {
                var onlinePlayers = await FactoryManager.Request_Player_List();
                if(!onlinePlayers.list.Contains(aPlayerId)) return NotFound($"Player {aPlayerId} not online");

                var PlayerInfo = await FactoryManager.Request_Player_Info(aPlayerId.ToId());

                var Result = new BlueprintResources()
                {
                    PlayerId = aPlayerId,
                    ItemStacks = new List<ItemStack>(),
                    ReplaceExisting = true,
                };
                PlayerInfo.bpResourcesInFactory?.ForEach(I => Result.ItemStacks.Add(new ItemStack() { id = I.Key, count = (int)I.Value }));
                return Ok(Result);
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
            return Ok(_db.FactoryItems.Where(B => B.Id == key).OrderByDescending(B => B.Timestamp));
        }

        [HttpPost("SetBlueprintResources")]
        [Authorize(Roles = nameof(Role.Moderator))]
        public async Task<IActionResult> SetBlueprintResources([FromBody]BlueprintResources aRessources)
        {
            try
            {
                await FactoryManager.Request_Blueprint_Resources(aRessources);
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