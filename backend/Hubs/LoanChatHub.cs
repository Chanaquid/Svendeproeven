using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
public class LoanChatHub : Hub
{
    private readonly ILoanMessageService _messageService;
    private readonly IOnlineTracker _onlineTracker;
    private readonly ILoanMessageRepository _loanMessageRepository;


    public LoanChatHub(ILoanMessageService messageService, IOnlineTracker onlineTracker, ILoanMessageRepository loanMessageRepository)
    {
        _messageService = messageService;
        _onlineTracker = onlineTracker;
        _loanMessageRepository = loanMessageRepository;
    }

    public async Task JoinLoan(int loanId)
    {
        var userId = Context.UserIdentifier;
        if (userId == null) return;

        var isAllowed = await _messageService.IsPartyToLoanAsync(loanId, userId);
        if (!isAllowed)
        {
            await Clients.Caller.SendAsync("Error", "Not allowed");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"loan_{loanId}");
        _onlineTracker.AddToLoan(Context.ConnectionId, userId, loanId);

        var unread = await _loanMessageRepository.GetUnreadMessagesForUserAsync(loanId, userId);
        if (unread.Any())
        {
            foreach (var msg in unread)
                msg.IsRead = true;

            await _loanMessageRepository.SaveChangesAsync();

            foreach (var msg in unread)
            {
                await Clients
                    .Group($"loan_{loanId}")
                    .SendAsync("MessageRead", msg.Id);
            }
        }
    }

    public async Task LeaveLoan(int loanId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"loan_{loanId}");
        _onlineTracker.Remove(Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _onlineTracker.Remove(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}