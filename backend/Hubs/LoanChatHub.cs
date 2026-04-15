using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace backend.Hubs
{
    [Authorize]
    public class LoanChatHub : Hub
    {
        private readonly ILoanMessageService _messageService;

        public LoanChatHub(ILoanMessageService messageService)
        {
            _messageService = messageService;
        }

        public async Task JoinLoan(int loanId)
        {
            var userId = Context.UserIdentifier;

            if (userId == null)
                return;

            var isAllowed = await _messageService.IsPartyToLoanAsync(loanId, userId);

            if (!isAllowed)
            {
                await Clients.Caller.SendAsync("Error", "Not allowed");
                return;
            }

            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"loan_{loanId}");
        }

        public async Task LeaveLoan(int loanId)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                $"loan_{loanId}");
        }
    }
}
