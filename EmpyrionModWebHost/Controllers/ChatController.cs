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

        public void SendMessage(string aChatTarget, string aChatAsUser, string aMessage)
        {
            ChatManager = Program.GetManager<ChatManager>();
            ChatManager?.ChatMessage(aChatTarget, aChatAsUser, aMessage);
        }
    }

    public class ChatManager : EmpyrionModBase, IEWAPlugin
    {
        public ModGameAPI GameAPI { get; private set; }
        public IHubContext<ChatHub> ChatHub { get; private set; }
        public PlayerManager PlayerManager { get; private set; }

        public ChatManager(IHubContext<ChatHub> aChatHub)
        {
            ChatHub = aChatHub;
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
                FactionName   = "???",
                Type          = aChatInfo.type,
                Message       = aChatInfo.msg,
            });
        }

        public async void AddChatToDB(Chat aChat)
        {
            using(var DB = new ChatContext())
            {
                DB.Database.EnsureCreated();
                DB.Add(aChat);
                DB.SaveChanges();
            }

            await ChatHub?.Clients.All.SendAsync("Send", JsonConvert.SerializeObject(aChat));
        }

        public async Task ChatMessage(string aChatTarget, string aChatAsUser, string aMessage)
        {
            if (string.IsNullOrEmpty(aMessage)) return;

            AddChatToDB(new Chat()
            {
                Timestamp     = DateTime.Now,
                PlayerSteamId = "",
                PlayerName    = string.IsNullOrEmpty(aChatAsUser) ? "Server" : aChatAsUser,
                FactionId     = 0,
                FactionName   = string.IsNullOrEmpty(aChatAsUser) ? "SERV" : aChatAsUser,
                Type          = (byte)(aChatTarget == null ? ChatType.Global : (aChatTarget.StartsWith("f:") ? ChatType.Faction : (aChatTarget.StartsWith("p:") ? ChatType.Private : ChatType.Global))),
                Message       = aMessage,
            });

            await Request_ConsoleCommand(new PString($"SAY {aChatTarget} '{(string.IsNullOrEmpty(aChatAsUser) ? "" : aChatAsUser + ": ")}{aMessage.Replace("'", "\\'")}'"));
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            LogLevel = EmpyrionNetAPIDefinitions.LogLevel.Debug;
            PlayerManager = Program.GetManager<PlayerManager>();

            Event_ChatMessage += ChatManager_Event_ChatMessage;
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
