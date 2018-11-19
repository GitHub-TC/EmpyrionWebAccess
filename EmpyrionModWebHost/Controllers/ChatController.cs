using Eleon.Modding;
using EmpyrionNetAPIAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace EmpyrionModWebHost.Controllers
{
    public class ChatDataModel
    {
        public string mark;
        public string type;
        public string timestamp;
        public string faction;
        public string toPlayer;
        public string playerName;
        public string message;
    }


    public class ChatHub : Hub
    {
        private ChatManager ChatManager { get; set; }

        public async Task SendMessage(string user, string message)
        {
            ChatManager = Program.GetManager<ChatManager>();

            Console.WriteLine($"Chat: {user}:{message}");
            ChatManager?.ChatMessage(message);
            await Clients.All.SendAsync("Send", "back:" + message);
        }
    }

    public class ChatManager : EmpyrionModBase, IEWAPlugin
    {
        public ModGameAPI GameAPI { get; private set; }
        
        public ChatManager(IHubContext<ChatHub> aChatHub)
        {
            ChatHub = aChatHub;
        }

        private void ChatManager_Event_ChatMessage(ChatInfo aChatInfo)
        {
            var chat = new ChatDataModel()
            {
                mark = GetMarkChatline(aChatInfo),
                type = GetChatType(aChatInfo.type),
                faction = GetFactionName(aChatInfo.recipientFactionId),
                toPlayer = GetPlayerName(aChatInfo.recipientEntityId),
                playerName = GetPlayerName(aChatInfo.playerId),
                timestamp = DateTime.Now.ToShortTimeString(),
                message = aChatInfo.msg,
            };
            ChatHub?.Clients.All.SendAsync("Send", JsonConvert.SerializeObject(chat)).Wait();
        }

        private string GetMarkChatline(ChatInfo aChatInfo)
        {
            return "N";
        }

        private string GetPlayerName(int aPlayerId)
        {
            return aPlayerId.ToString();
        }

        private string GetFactionName(int aFactionId)
        {
            return aFactionId.ToString();
        }

        private string GetChatType(byte type)
        {
            return "N";
        }

        public void ChatMessage(String aMessage)
        {
            Request_ConsoleCommand(new PString($"SAY '{aMessage}'"));
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            LogLevel = EmpyrionNetAPIDefinitions.LogLevel.Debug;

            Event_ChatMessage += ChatManager_Event_ChatMessage;
        }

        public IHubContext<ChatHub> ChatHub { get; internal set; }
    }

    [Route("api/[controller]")]
    public class ChatController
    {
        public IHubContext<ChatHub> ChatHub { get; }
        public ChatManager ChatManager { get; }

        public ChatController(IHubContext<ChatHub> aChatHub)
        {
            ChatHub = aChatHub;
            ChatManager = Program.GetManager<ChatManager>();
            ChatManager.ChatHub = aChatHub;
        }

    }
}
