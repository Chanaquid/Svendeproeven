using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace backend.Hubs
{
    [Authorize]
    public class SupportChatHub : Hub
    {
        private readonly ISupportService _supportService;

        public SupportChatHub(ISupportService supportService)
        {
            _supportService = supportService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (userId != null)
            {
                // Each user joins their own group to receive thread updates
                await Groups.AddToGroupAsync(Context.ConnectionId, $"support_user_{userId}");
            }
            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            return base.OnDisconnectedAsync(exception);
        }

        public async Task JoinThread(int threadId)
        {
            var userId = Context.UserIdentifier;
            if (userId == null) return;

            var canAccess = await _supportService.CanUserAccessThreadAsync(threadId, userId);
            if (!canAccess)
            {
                await Clients.Caller.SendAsync("Error", "Not allowed.");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"support_thread_{threadId}");
        }

        public async Task LeaveThread(int threadId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"support_thread_{threadId}");
        }
    }
}