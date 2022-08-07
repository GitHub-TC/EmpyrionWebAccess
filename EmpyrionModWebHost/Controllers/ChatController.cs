
using NuGet.Protocol.Plugins;
using System.Threading.Channels;
using static System.Net.Mime.MediaTypeNames;

namespace EmpyrionModWebHost.Controllers
{

    enum ChatType
    {
        Global = 3,
        Faction = 5,
        Private = 1,
    }


    [Authorize(Roles = nameof(Role.VIP))]
    public class ChatHub : RoleHubBase
    {
        public IUserService UserService { get; set; }
        private ChatManager ChatManager { get; set; }
        public PlayerManager PlayerManager { get; set; }
        public FactionManager FactionManager { get; set; }

        public ChatHub(IUserService aUserService) 
        {
            UserService    = aUserService;
            ChatManager    = Program.GetManager<ChatManager>();
            PlayerManager  = Program.GetManager<PlayerManager>();
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
            _ = ChatManager.ChatMessage(Context.User, aChatTarget, aChatTargetHint, aChatAsUser, FactionName, aMessage);
        }
    }

    public class ChatManager : EmpyrionModBase, IEWAPlugin, IDatabaseConnect
    {
        public ModGameAPI GameAPI { get; set; }
        public IRoleHubContext<ChatHub> ChatHub { get; set; }
        public PlayerManager PlayerManager { get; set; }
        public FactionManager FactionManager { get; set; }

        public ChatManager(IRoleHubContext<ChatHub> aChatHub)
        {
            ChatHub = aChatHub;
        }

        public void CreateAndUpdateDatabase()
        {
            using var DB = new ChatContext();
            DB.Database.Migrate();
            DB.Database.EnsureCreated();
            DB.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        }

        private void ChatManager_Event_ChatMessage(ChatInfo aChatInfo)
        {
            var player = PlayerManager.GetPlayer(aChatInfo.playerId);

            AddChatToDB(player, new Chat()
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

        public void AddChatToDB(Player aPlayer, Chat aChat)
        {
            using (var DB = new ChatContext())
            {
                DB.Add(aChat);
                DB.SaveChanges();
            }

            if(aChat.Type == (byte)ChatType.Global) ChatHub?.Clients.All.SendAsync(         "Send", JsonConvert.SerializeObject(aChat));
            else                                    ChatHub?.RoleSendAsync        (aPlayer, "Send", JsonConvert.SerializeObject(aChat));
        }

        public async Task ChatMessage(ClaimsPrincipal user, string aChatTarget, string aChatTargetHint, string aChatAsUser, string aFactionName, string aMessage)
        {
            if (string.IsNullOrEmpty(aMessage)) return;

            AddChatToDB(null, new Chat()
            {
                Timestamp     = DateTime.Now,
                PlayerSteamId = "",
                PlayerName    = string.IsNullOrEmpty(aChatAsUser) ? "Server" : aChatAsUser,
                FactionId     = 0,
                FactionName   = aFactionName,
                Type          = (byte)(aChatTarget == null ? ChatType.Global : (aChatTarget.StartsWith("f:") ? ChatType.Faction : (aChatTarget.StartsWith("p:") ? ChatType.Private : ChatType.Global))),
                Message       = $"{aChatTargetHint}{RemoveBBCode(aMessage)}",
            });

            var msg = new Eleon.MessageData { 
                SenderNameOverride = string.IsNullOrEmpty(aChatAsUser) ? "EWA" : aChatAsUser,
                SenderType         = Eleon.SenderType.ServerPrio,
                IsTextLocaKey      = IsLocalizationText(ref aMessage, out var arg1, out var arg2),
                Text               = aMessage,
                Arg1               = arg1,
                Arg2               = arg2
            };

            if (aChatTarget?.StartsWith("p:") == true)
            {
                msg.Channel           = Eleon.MsgChannel.SinglePlayer;
                msg.RecipientEntityId = int.TryParse(aChatTarget.AsSpan(2), out var entityId) ? entityId : -1;
            }
            else if (aChatTarget?.StartsWith("f:") == true)
            {
                FactionManager.QueryFaction(DB =>
                DB.Factions.Where(
                    P => P.Abbrev == aChatTarget.Substring(2)),
                    P =>
                    {
                        msg.Channel          = Eleon.MsgChannel.Faction;
                        msg.RecipientFaction = new FactionData { Id = P.FactionId, Group = FactionGroup.Faction };
                    });
            }

            await Request_SendChatMessage(msg);
        }

        public async Task ChatMessageSERV(string aMessage)
        {
            if (string.IsNullOrEmpty(aMessage)) return;

            AddChatToDB(null, new Chat()
            {
                Timestamp     = DateTime.Now,
                PlayerSteamId = "",
                PlayerName    = "Server",
                FactionId     = 0,
                FactionName   = "SERV",
                Type          = (byte)ChatType.Global,
                Message       = $"{RemoveBBCode(aMessage)}",
            });

            // Eleon.MsgChannel.Server funktioniert leider nicht Stand 1.8.7 - 3863
            //await Request_SendChatMessage(new Eleon.MessageData
            //{
            //    Channel            = Eleon.MsgChannel.Server,
            //    SenderNameOverride = "SERVER",
            //    SenderType         = Eleon.SenderType.ServerPrio,
            //    Text               = aMessage
            //});

            await Request_ConsoleCommand(new PString($"SAY '{($"[c][ffffff]{aMessage}[/c]".Replace("'", "\\'"))}'"));
        }

        public async Task ChatMessageGlobal(string aMessage)
        {
            if (string.IsNullOrEmpty(aMessage)) return;

            AddChatToDB(null, new Chat()
            {
                Timestamp     = DateTime.Now,
                PlayerSteamId = "",
                PlayerName    = "Server",
                FactionId     = 0,
                FactionName   = "SERV",
                Type          = (byte)ChatType.Global,
                Message       = $"{RemoveBBCode(aMessage)}",
            });

            await Request_SendChatMessage(new Eleon.MessageData
            {
                Channel             = Eleon.MsgChannel.Global,
                SenderNameOverride  = "SERVER",
                SenderType          = Eleon.SenderType.ServerPrio,
                IsTextLocaKey       = IsLocalizationText(ref aMessage, out var arg1, out var arg2),
                Text                = aMessage,
                Arg1                = arg1,
                Arg2                = arg2
            });
        }

        public async Task ChatMessageADM(string aMessage)
        {
            if (string.IsNullOrEmpty(aMessage)) return;

            AddChatToDB(null, new Chat()
            {
                Timestamp     = DateTime.Now,
                PlayerSteamId = "",
                PlayerName    = "-ADM-",
                FactionId     = 0,
                FactionName   = "-ADM-",
                Type          = (byte)ChatType.Global,
                Message       = $"{RemoveBBCode(aMessage)}",
            });

            await Request_SendChatMessage(new Eleon.MessageData
            {
                Channel            = Eleon.MsgChannel.Global,
                SenderNameOverride = "-ADM-",
                SenderType         = Eleon.SenderType.ServerPrio,
                IsTextLocaKey      = IsLocalizationText(ref aMessage, out var arg1, out var arg2),
                Text               = aMessage,
                Arg1               = arg1,
                Arg2               = arg2
            });
        }

        private bool IsLocalizationText(ref string response, out string arg1, out string arg2)
        {
            arg1 = arg2 = null;
            if (string.IsNullOrEmpty(response) || !response.StartsWith("|")) return false;

            var parts = response.Split('|');
            response = parts[1];
            if (parts.Length > 2) arg1 = parts[2];
            if (parts.Length > 3) arg2 = parts[3];

            return true;
        }

        string RemoveBBCode(string aMessage)
        {
            return string.Join("", aMessage.Split('[').Select(S => S.Contains(']') ? S.Substring(S.IndexOf(']') + 1) : S));
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;
            LogLevel       = EmpyrionNetAPIDefinitions.LogLevel.Debug;
            PlayerManager  = Program.GetManager<PlayerManager>();
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
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("content-type", "application/json");
            Stream data = client.GetStreamAsync(aData.CallUrl).Result;
            using StreamReader messageReader = new StreamReader(data);
            return Ok(messageReader.ReadToEnd());
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

        public static ODataConventionModelBuilder GetEdmModel(ODataConventionModelBuilder builder)
        {
            builder.EntitySet<Chat>("Chats");
            return builder;
        }

    }
}
