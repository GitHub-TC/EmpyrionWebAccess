using System;
using EmpyrionModWebHost.Models;
using Microsoft.AspNetCore.SignalR;
using EmpyrionModWebHost.Services;
using System.Threading.Tasks;
using System.Threading;

namespace EmpyrionModWebHost.Controllers
{
    //
    // Summary:
    //     A context abstraction for a hub.
    public interface IRoleHubContext<THub> : IHubContext<THub> where THub : Hub
    {
        void RoleSendAsync(Player aCurrentPlayer, string aMethod, object aArg1, CancellationToken cancellationToken = default(CancellationToken));
    }

    public class RoleHubBase : Hub {
        public const string AdminsGroupName = "Admin";

        public override Task OnConnectedAsync()
        {
            var PlayerManager = Program.GetManager<PlayerManager>();
            PlayerManager.AddConnectionAsync(Context.ConnectionId, Context.User, Groups);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var PlayerManager = Program.GetManager<PlayerManager>();
            PlayerManager.RemoveConnectionAsync(Context.ConnectionId, Context.User, Groups);
            return base.OnDisconnectedAsync(exception);
        }

    }

    public class RoleHub<THub> : IRoleHubContext<THub> where THub : Hub
    {
        public IHubContext<THub> HubContext { get; }
        public IProvider<IUserService> ContextUserService { get; }

        public IHubClients Clients => HubContext.Clients;

        public IGroupManager Groups => HubContext.Groups;

        public RoleHub(IProvider<IUserService> aUserService, IHubContext<THub> aHubContext)
        {
            HubContext = aHubContext;
            ContextUserService = aUserService;
        }

        public async void RoleSendAsync(Player aCurrentPlayer, string aMethod, object aArg1, CancellationToken cancellationToken = default(CancellationToken))
        {
            await HubContext?.Clients?.Groups(RoleHubBase.AdminsGroupName)?.SendAsync(aMethod, aArg1);

            var CurrentPlayer = aCurrentPlayer;
            User CurrentUser = null;

            if (CurrentPlayer == null)
            {
                var PlayerManager = Program.GetManager<PlayerManager>();
                CurrentUser = ContextUserService.Get()?.CurrentUser;
                if (CurrentUser == null || PlayerManager == null) return;
                CurrentPlayer = PlayerManager.GetPlayer(CurrentUser.InGameSteamId);
            }

            if (CurrentUser == null)
            {
                var UserService = Program.GetManager<UserManager>();
                if (UserService == null) return;
                CurrentUser = UserService.GetBySteamId(CurrentPlayer.SteamId);
            }

            if ((CurrentUser.Role == Role.Player && CurrentPlayer.SteamId == CurrentUser.InGameSteamId) ||
                (CurrentUser.Role == Role.VIP    && CurrentPlayer.FactionGroup == (byte)Factions.Private))
            {
                await HubContext?.Clients?.Groups(CurrentUser.InGameSteamId)?.SendAsync(aMethod, aArg1);
            }
            else await HubContext?.Clients?.Groups(CurrentPlayer.FactionId.ToString())?.SendAsync(aMethod, aArg1);
        }
    }
}