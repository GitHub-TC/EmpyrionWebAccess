using Eleon.Modding;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace EmpyrionModWebHost.Controllers
{
    public class ChatDataModel
    {
        public string Message;
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

//    [Export(typeof(IEWAPlugin))]
    public class ChatManager : IEWAPlugin
    {
        public ModGameAPI GameAPI { get; private set; }
        
        public ChatManager(IHubContext<ChatHub> aChatHub)
        {
            ChatHub = aChatHub;
        }

        public void Game_Event(CmdId eventId, ushort seqNr, object data)
        {
            if (eventId != CmdId.Event_ChatMessage) return;

            var Msg = data as Eleon.Modding.ChatInfo;
            ChatHub?.Clients.All.SendAsync("Send", $"back:{Msg.playerId}:{Msg.msg}").Start();
        }

        public void Game_Exit()
        {
        }

        public void Game_Start(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
        }

        public void Game_Update()
        {
        }

        public void SendChat(ChatDataModel aChatDataModel)
        {
            GameAPI?.Game_Request(CmdId.Request_InGameMessage_AllPlayers, 1, new ChatInfo() { msg = aChatDataModel.Message });
        }

        public void ChatMessage(String msg)
        {
            String command = "SAY '" + msg + "'";
            GameAPI.Game_Request(
                CmdId.Request_ConsoleCommand, 
                (ushort)CmdId.Request_InGameMessage_AllPlayers, 
                new Eleon.Modding.PString(command));
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
