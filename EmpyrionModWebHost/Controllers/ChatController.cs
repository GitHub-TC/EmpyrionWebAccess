using Eleon.Modding;
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
using System.IO;

namespace EmpyrionModWebHost.Controllers
{

    enum ChatType
    {
        Global = 3,
        Faction = 5,
        Private = 1,
    }


    [Authorize]
    public class ChatHub : Hub
    {
        private ChatManager ChatManager { get; set; }

        public void SendMessage(string aChatTarget, string aChatTargetHint, string aChatAsUser, string aMessage)
        {
            ChatManager = Program.GetManager<ChatManager>();
            ChatManager?.ChatMessage(aChatTarget, aChatTargetHint, aChatAsUser, aMessage);
        }
    }

    public class ChatManager : EmpyrionModBase, IEWAPlugin, IDatabaseConnect
    {
        public ModGameAPI GameAPI { get; private set; }
        public IHubContext<ChatHub> ChatHub { get; private set; }
        public PlayerManager PlayerManager { get; private set; }
        public FactionManager FactionManager { get; private set; }

        public ChatManager(IHubContext<ChatHub> aChatHub)
        {
            ChatHub = aChatHub;
        }

        public void CreateAndUpdateDatabase()
        {
            using (var DB = new ChatContext())
            {
                DB.Database.Migrate();
                DB.Database.EnsureCreated();
            }
        }

        private void ChatManager_Event_ChatMessage(ChatInfo aChatInfo)
        {
            var player = PlayerManager.GetPlayer(aChatInfo.playerId);

            AddChatToDB(new Chat()
            {
                Timestamp     = DateTime.Now,
                PlayerSteamId = player?.SteamId,
                PlayerName    = player?.PlayerName,
                FactionId     = player == null ? 0 : player.FactionId,
                FactionName   = FactionManager.GetFaction(player.FactionId)?.Abbrev,
                Type          = aChatInfo.type,
                Message       = aChatInfo.msg,
            });
        }

        public async void AddChatToDB(Chat aChat)
        {
            using(var DB = new ChatContext())
            {
                DB.Add(aChat);
                DB.SaveChanges();
            }

            await ChatHub?.Clients.All.SendAsync("Send", JsonConvert.SerializeObject(aChat));
        }

        public void ChatMessage(string aChatTarget, string aChatTargetHint, string aChatAsUser, string aMessage)
        {
            if (string.IsNullOrEmpty(aMessage)) return;

            ChatMessage(aChatTarget, aChatTargetHint, aChatAsUser, string.IsNullOrEmpty(aChatAsUser) ? "" : aChatAsUser + ": ", aMessage);
        }

        public void ChatMessage(string aChatTarget, string aChatTargetHint, string aChatAsUser, string aMessagePrefix, string aMessage)
        {
            if (string.IsNullOrEmpty(aMessage)) return;

            AddChatToDB(new Chat()
            {
                Timestamp     = DateTime.Now,
                PlayerSteamId = "",
                PlayerName    = string.IsNullOrEmpty(aChatAsUser) ? "Server" : aChatAsUser,
                FactionId     = 0,
                FactionName   = string.IsNullOrEmpty(aChatAsUser) ? "SERV" : "-ADM-",
                Type          = (byte)(aChatTarget == null ? ChatType.Global : (aChatTarget.StartsWith("f:") ? ChatType.Faction : (aChatTarget.StartsWith("p:") ? ChatType.Private : ChatType.Global))),
                Message       = $"{aChatTargetHint}{aMessage}",
            });

            Request_ConsoleCommand(new PString($"SAY {aChatTarget} '{aMessagePrefix}{aMessage.Replace("'", "\\'")}'"));
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            LogLevel = EmpyrionNetAPIDefinitions.LogLevel.Debug;
            PlayerManager = Program.GetManager<PlayerManager>();
            FactionManager = Program.GetManager<FactionManager>();

            Event_ChatMessage += ChatManager_Event_ChatMessage;
        }
    }

    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class ChatsApiController : ControllerBase
    {
        public class TanslateData
        {
            public string CallUrl { get; set; }
        }

        [HttpPost("Translate")]
        public ActionResult<string> Translate([FromBody]TanslateData aData)
        {
            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                client.Headers.Add("content-type", "application/json");
                Stream data = client.OpenRead(aData.CallUrl);
                using (StreamReader messageReader = new StreamReader(data))
                {
                    return Ok(messageReader.ReadToEnd());
                }
            }
        }

    }

    [Authorize]
    public class ChatsController: ODataController
    {
        public ChatContext DB { get; }
        public ChatManager ChatManager { get; }

        public ChatsController(ChatContext aChatContext)
        {
            DB = aChatContext;
            ChatManager = Program.GetManager<ChatManager>();
        }

        [EnableQuery]
        public IActionResult Get()
        {
            return Ok(DB.Chats);
        }

        public static IEdmModel GetEdmModel()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<Chat>("Chats");
            return builder.GetEdmModel();
        }

    }
}
