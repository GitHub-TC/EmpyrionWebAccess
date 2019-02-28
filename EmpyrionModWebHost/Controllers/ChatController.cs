using Eleon.Modding;
using EmpyrionModWebHost.Models;
using EmpyrionModWebHost.Services;
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
using System.Linq;

namespace EmpyrionModWebHost.Controllers
{

    enum ChatType
    {
        Global = 3,
        Faction = 5,
        Private = 1,
    }


    [Authorize(Roles = nameof(Role.VIP))]
    public class ChatHub : Hub
    {
        public IUserService UserService { get; }
        private ChatManager ChatManager { get; set; }
        public PlayerManager PlayerManager { get; }
        public FactionManager FactionManager { get; }

        public ChatHub(IUserService aUserService)
        {
            UserService = aUserService;
            ChatManager = Program.GetManager<ChatManager>();
            PlayerManager = Program.GetManager<PlayerManager>();
            FactionManager = Program.GetManager<FactionManager>();
        }

        public void SendMessage(string aChatTarget, string aChatTargetHint, string aChatAsUser, string aMessage)
        {
            var FactionName = string.IsNullOrEmpty(aChatAsUser) ? "SERV" : "-ADM-";
            if (!Context.User.IsInRole(nameof(Role.GameMaster)))
            {
                var userId = int.Parse(Context.User.Identity.Name);
                var user = UserService.GetById(userId);

                aChatAsUser = user.Username;
                PlayerManager.QueryPlayer(DB =>
                    DB.Players.Where(
                        P => P.SteamId == user.InGameSteamId),
                        P => FactionName = $"*{FactionManager.GetFaction(P.FactionId)?.Abbrev}");
            }
            ChatManager.ChatMessage(aChatTarget, aChatTargetHint, aChatAsUser, FactionName, aMessage);
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

        public void ChatMessage(string aChatTarget, string aChatTargetHint, string aChatAsUser, string aFactionName, string aMessage)
        {
            if (string.IsNullOrEmpty(aMessage)) return;

            AddChatToDB(new Chat()
            {
                Timestamp = DateTime.Now,
                PlayerSteamId = "",
                PlayerName = string.IsNullOrEmpty(aChatAsUser) ? "Server" : aChatAsUser,
                FactionId = 0,
                FactionName = aFactionName,
                Type = (byte)(aChatTarget == null ? ChatType.Global : (aChatTarget.StartsWith("f:") ? ChatType.Faction : (aChatTarget.StartsWith("p:") ? ChatType.Private : ChatType.Global))),
                Message = $"{aChatTargetHint}{RemoveBBCode(aMessage)}",
            });

            Request_ConsoleCommand(new PString($"SAY {aChatTarget} '{(string.IsNullOrEmpty(aChatAsUser) ? "" : $"[c][ff8000]{aChatAsUser}: [/c]")}{($"[c][ffffff]{aMessage}[/c]".Replace("'", "\\'"))}'"));
        }

        public void ChatMessageSERV(string aMessage)
        {
            if (string.IsNullOrEmpty(aMessage)) return;

            AddChatToDB(new Chat()
            {
                Timestamp = DateTime.Now,
                PlayerSteamId = "",
                PlayerName = "Server",
                FactionId = 0,
                FactionName = "SERV",
                Type = (byte)ChatType.Global,
                Message = $"{RemoveBBCode(aMessage)}",
            });

            Request_ConsoleCommand(new PString($"SAY '{($"[c][ffffff]{aMessage}[/c]".Replace("'", "\\'"))}'"));
        }

        public void ChatMessageADM(string aMessage)
        {
            if (string.IsNullOrEmpty(aMessage)) return;

            AddChatToDB(new Chat()
            {
                Timestamp     = DateTime.Now,
                PlayerSteamId = "",
                PlayerName    = "-ADM-",
                FactionId     = 0,
                FactionName   = "-ADM-",
                Type          = (byte)ChatType.Global,
                Message       = $"{RemoveBBCode(aMessage)}",
            });

            Request_ConsoleCommand(new PString($"SAY '{aMessage.Replace("'", "\\'")}'"));
        }

        string RemoveBBCode(string aMessage)
        {
            return string.Join("", aMessage.Split('[').Select(S => S.Contains(']') ? S.Substring(S.IndexOf(']') + 1) : S));
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

    [ApiController]
    [Authorize(Roles = nameof(Role.VIP))]
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
        public IUserService UserService { get; }
        public ChatContext DB { get; }
        public PlayerManager PlayerManager { get; }
        public ChatManager ChatManager { get; }

        public ChatsController(IUserService aUserService, ChatContext aChatContext)
        {
            UserService = aUserService;
            DB = aChatContext;
            PlayerManager = Program.GetManager<PlayerManager>();
            ChatManager = Program.GetManager<ChatManager>();
        }

        [EnableQuery]
        public IActionResult Get()
        {
            switch (UserService.CurrentUser.Role)
            {
                case Role.ServerAdmin:
                case Role.InGameAdmin:
                case Role.Moderator:
                case Role.GameMaster:   return Ok(DB.Chats);
                case Role.VIP:          var Faction = PlayerManager.CurrentPlayer?.FactionId;
                                        return Ok(DB.Chats.Where(P => P.FactionId == Faction || P.Type == (byte)ChatType.Global));
                case Role.Player:       return Ok(DB.Chats.Where(P => P.Type == (byte)ChatType.Global));
                case Role.None: return Ok();
                default:        return Ok();
            }
        }

        public static IEdmModel GetEdmModel()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<Chat>("Chats");
            return builder.GetEdmModel();
        }

    }
}
