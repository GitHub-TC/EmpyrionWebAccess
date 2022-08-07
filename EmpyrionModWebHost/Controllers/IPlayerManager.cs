using System.Collections.Concurrent;

namespace EmpyrionModWebHost.Controllers
{
    public interface IPlayerManager
    {
        Lazy<ChatManager> ChatManager { get; }
        Player CurrentPlayer { get; }
        ModGameAPI GameAPI { get; }
        ILogger<PlayerManager> Logger { get; set; }
        FileSystemWatcher mPlayersDirectoryFileWatcher { get; }
        int OnlinePlayersCount { get; }
        IRoleHubContext<PlayerHub> PlayerHub { get; }
        string PlayersDirectory { get; }
        Lazy<SysteminfoManager> SysteminfoManager { get; }
        ConcurrentDictionary<string, Player> UpdatePlayersQueue { get; set; }
        Lazy<UserManager> UserManager { get; }
        IProvider<IUserService> UserService { get; }

        void AddConnectionAsync(string aConnectionId, ClaimsPrincipal aUser, IGroupManager aGroups);
        void ChangePlayerInfo(PlayerInfoSet aSet);
        void ChangePlayerNote(string aSteamId, string aNote);
        void CreateAndUpdateDatabase();
        Player GetPlayer(int aPlayerId);
        Player GetPlayer(string aSteamId);
        void Initialize(ModGameAPI dediAPI);
        void QueryPlayer(Func<PlayerContext, IEnumerable<Player>> aSelect, Action<Player> aAction);
        void RemoveConnectionAsync(string aConnectionId, ClaimsPrincipal aUser, IGroupManager aGroups);
    }
}