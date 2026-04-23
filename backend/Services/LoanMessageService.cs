using backend.Dtos;
using backend.Hubs;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace backend.Services
{
    public class LoanMessageService : ILoanMessageService
    {
        private readonly ILoanMessageRepository _loanMessageRepository;
        private readonly ILoanRepository _loanRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<LoanChatHub> _hubContext;
        private readonly IOnlineTracker _onlineTracker;
        private readonly UserManager<ApplicationUser> _userManager;

        private const int LockDaysAfterCompletion = 7;

        public LoanMessageService(
            ILoanMessageRepository loanMessageRepository,
            ILoanRepository loanRepository,
            IUserRepository userRepository,
            INotificationService notificationService,
            IHubContext<LoanChatHub> hubContext,
            IOnlineTracker onlineTracker,
            UserManager<ApplicationUser> userManager)
        {
            _loanMessageRepository = loanMessageRepository;
            _loanRepository = loanRepository;
            _userRepository = userRepository;
            _notificationService = notificationService;
            _hubContext = hubContext;
            _onlineTracker = onlineTracker;
            _userManager = userManager;
        }

        public async Task<LoanMessageDto> SendMessageAsync(int loanId, string senderId, SendLoanMessageDto dto, bool isAdmin)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(loanId)
                ?? throw new KeyNotFoundException("Loan not found.");

            EnsureMessagingAllowed(loan, senderId, isAdmin);

            var message = new LoanMessage
            {
                LoanId = loanId,
                SenderId = senderId,
                Content = dto.Content.Trim(),
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            await _loanMessageRepository.AddAsync(message);
            await _loanMessageRepository.SaveChangesAsync();

            // Resolve sender — admin may not be borrower/lender nav property
            ApplicationUser? sender = isAdmin && loan.BorrowerId != senderId && loan.LenderId != senderId
                ? await _userRepository.GetByIdAsync(senderId)
                : loan.BorrowerId == senderId ? loan.Borrower : loan.Lender;

            var result = new LoanMessageDto
            {
                Id = message.Id,
                LoanId = loanId,
                SenderId = senderId,
                SenderName = sender?.FullName ?? string.Empty,
                SenderAvatarUrl = sender?.AvatarUrl,
                IsMine = true,
                Content = message.Content,
                IsRead = false,
                SentAt = message.SentAt
            };

            // Broadcast to all connections in the loan group
            await _hubContext.Clients
                .Group($"loan_{loanId}")
                .SendAsync("ReceiveMessage", result);

            // Determine the other party (admins who are not a party have no "other party" to notify)
            var isParty = loan.BorrowerId == senderId || loan.LenderId == senderId;
            var otherPartyId = isParty
                ? (loan.BorrowerId == senderId ? loan.LenderId : loan.BorrowerId)
                : null;

            if (otherPartyId != null)
            {
                var otherPartyOnline = _onlineTracker.IsUserInLoanGroup(otherPartyId, loanId);

                if (otherPartyOnline)
                {
                    // Other party is viewing the chat — mark as read immediately
                    message.IsRead = true;
                    await _loanMessageRepository.SaveChangesAsync();

                    await _hubContext.Clients
                        .Group($"loan_{loanId}")
                        .SendAsync("MessagesRead", message.Id);
                }
                else
                {
                    await _notificationService.SendAsync(
                        otherPartyId,
                        NotificationType.LoanMessageReceived,
                        $"New message from {sender?.FullName} about '{loan.Item?.Title}'.",
                        loanId,
                        NotificationReferenceType.Loan
                    );

                    // Real-time toast so they see a red dot on other pages
                    await _hubContext.Clients
                        .Group($"user_{otherPartyId}")
                        .SendAsync("NewMessageNotification", new
                        {
                            LoanId = loanId,
                            ItemTitle = loan.Item?.Title,
                            From = sender?.FullName
                        });
                }
            }

            return result;
        }

        public async Task<PagedResult<LoanMessageDto>> GetMessagesAsync(
            int loanId,
            string userId,
            bool isAdmin,
            PagedRequest request)
        {
            var loan = await _loanRepository.GetByIdAsync(loanId)
                ?? throw new KeyNotFoundException("Loan not found.");

            if (!isAdmin && loan.BorrowerId != userId && loan.LenderId != userId)
                throw new UnauthorizedAccessException("You do not have access to this loan's messages.");

            var paged = await _loanMessageRepository.GetByLoanIdAsync(loanId, request);

            // Mark incoming messages as read (only for actual parties, not admins)
            if (!isAdmin)
            {
                var unread = paged.Items.Where(m => m.SenderId != userId && !m.IsRead).ToList();
                if (unread.Any())
                {
                    foreach (var m in unread)
                        m.IsRead = true;

                    await _loanMessageRepository.SaveChangesAsync();

                    foreach (var m in unread)
                    {
                        await _hubContext.Clients
                            .Group($"loan_{loanId}")
                            .SendAsync("MessageRead", m.Id);
                    }
                }
            }

            return new PagedResult<LoanMessageDto>
            {
                Items = paged.Items.Select(m => new LoanMessageDto
                {
                    Id = m.Id,
                    LoanId = m.LoanId,
                    SenderId = m.SenderId,
                    SenderName = m.Sender?.FullName ?? string.Empty,
                    SenderAvatarUrl = m.Sender?.AvatarUrl,
                    IsMine = m.SenderId == userId,
                    Content = m.Content,
                    IsRead = m.IsRead,
                    SentAt = m.SentAt
                }).ToList(),
                TotalCount = paged.TotalCount,
                Page = paged.Page,
                PageSize = paged.PageSize
            };
        }

        public async Task MarkAsReadAsync(int loanId, string userId, MarkLoanMessagesReadDto dto)
        {
            var loan = await _loanRepository.GetByIdAsync(loanId)
                ?? throw new KeyNotFoundException("Loan not found.");

            if (loan.BorrowerId != userId && loan.LenderId != userId)
                throw new UnauthorizedAccessException("You do not have access to this loan's messages.");

            await _loanMessageRepository.MarkAsReadAsync(loanId, userId, dto.UpToMessageId);

            // Push read receipt so the sender's UI updates the checkmark
            await _hubContext.Clients
                .Group($"loan_{loanId}")
                .SendAsync("MessagesRead", dto.UpToMessageId);
        }

        public async Task<int> GetUnreadCountAsync(int loanId, string userId)
        {
            var loan = await _loanRepository.GetByIdAsync(loanId)
                ?? throw new KeyNotFoundException("Loan not found.");

            if (loan.BorrowerId != userId && loan.LenderId != userId)
                throw new UnauthorizedAccessException("You do not have access to this loan.");

            return await _loanMessageRepository.GetUnreadCountAsync(loanId, userId);
        }

        public async Task<bool> IsPartyToLoanAsync(int loanId, string userId)
        {
            var loan = await _loanRepository.GetByIdAsync(loanId);
            if (loan == null) return false;

            if (loan.BorrowerId == userId || loan.LenderId == userId)
                return true;

            // Admins can join for real-time viewing
            var user = await _userManager.FindByIdAsync(userId);
            return user != null && await _userManager.IsInRoleAsync(user, "Admin");
        }

        // Helpers
        private static void EnsureMessagingAllowed(Loan loan, string senderId, bool isAdmin)
        {
            if (loan.Status == LoanStatus.Rejected || loan.Status == LoanStatus.Cancelled)
                throw new InvalidOperationException("Messaging is not available for rejected or cancelled loans.");

            if (loan.Status == LoanStatus.Completed && loan.ActualReturnDate.HasValue)
            {
                var lockDate = loan.ActualReturnDate.Value.AddDays(LockDaysAfterCompletion);
                if (DateTime.UtcNow >= lockDate)
                    throw new InvalidOperationException("The message thread for this loan has been locked.");
            }

            bool isParty = loan.BorrowerId == senderId || loan.LenderId == senderId;

            if (isAdmin && !isParty)
            {
                if (loan.Status != LoanStatus.AdminPending)
                    throw new InvalidOperationException("Admins can only send messages while the loan is pending admin review.");
                return;
            }

            if (loan.Status == LoanStatus.AdminPending && !isParty)
                throw new InvalidOperationException("Messaging is not available while the loan is pending admin review.");

            if (!isParty)
                throw new UnauthorizedAccessException("Only the borrower and lender can send messages on this loan.");
        }
    }
}