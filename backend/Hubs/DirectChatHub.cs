using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace backend.Hubs
{
    [Authorize]
    public class DirectChatHub : Hub
    {
        private readonly IDirectConversationService _conversationService;
        private readonly IOnlineTracker _onlineTracker;

        public DirectChatHub(
            IDirectConversationService conversationService,
            IOnlineTracker onlineTracker)
        {
            _conversationService = conversationService;
            _onlineTracker = onlineTracker;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (userId != null)
            {
                //Each user joins their own group so they receive sidebar updates
                //from any conversation, not just ones they're actively viewing
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            //Remove from tracker so online status is accurate
            _onlineTracker.Remove(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        public async Task JoinConversation(int conversationId)
        {
            var userId = Context.UserIdentifier;
            if (userId == null) return;

            var canAccess = await _conversationService.CanUserAccessConversationAsync(conversationId, userId);
            if (!canAccess)
            {
                await Clients.Caller.SendAsync("Error", "Not allowed.");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");

            //Track so SendMessageAsync knows if recipient is online
            _onlineTracker.AddToDirectChat(Context.ConnectionId, userId, conversationId);
        }

        public async Task LeaveConversation(int conversationId)
        {
            _onlineTracker.Remove(Context.ConnectionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        }
    }
}