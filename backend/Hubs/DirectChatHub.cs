using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace backend.Hubs
{
    [Authorize]
    public class DirectChatHub : Hub
    {
        private readonly IDirectConversationService _conversationService;

        public DirectChatHub(IDirectConversationService conversationService)
        {
            _conversationService = conversationService;
        }
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
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
        }

        public async Task LeaveConversation(int conversationId)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                $"conversation_{conversationId}");
        }
    }
}
