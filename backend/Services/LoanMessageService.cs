using backend.Dtos;
using backend.Interfaces;
using backend.Models;

namespace backend.Services
{
    public class LoanMessageService : ILoanMessageService
    {
        private readonly ILoanMessageRepository _loanMessageRepository;
        private readonly ILoanRepository _loanRepository;
        private readonly IUserRepository _userRepository;

        // Loan messages lock this many days after completion
        private const int LockDaysAfterCompletion = 7;

        public LoanMessageService(
            ILoanMessageRepository loanMessageRepository,
            ILoanRepository loanRepository,
            IUserRepository userRepository)
        {
            _loanMessageRepository = loanMessageRepository;
            _loanRepository = loanRepository;
            _userRepository = userRepository;
        }

        public async Task<LoanMessageDto> SendMessageAsync(int loanId, string senderId, SendLoanMessageDto dto, bool isAdmin)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(loanId)
                ?? throw new KeyNotFoundException("Loan not found.");

            //Checks if the loan status or time elapsed forbids messaging
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

            // Admin has no Borrower/Lender nav — fetch separately
            ApplicationUser? sender = isAdmin
                ? await _userRepository.GetByIdAsync(senderId)
                : loan.BorrowerId == senderId ? loan.Borrower : loan.Lender;

            return new LoanMessageDto
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
        }

        public async Task<PagedResult<LoanMessageDto>> GetMessagesAsync(
            int loanId,
            string userId,
            bool isAdmin,
            PagedRequest request)
        {
            var loan = await _loanRepository.GetByIdAsync(loanId)
                ?? throw new KeyNotFoundException("Loan not found.");

            //Admins can view, borrower and lender can view
            if (!isAdmin && loan.BorrowerId != userId && loan.LenderId != userId)
                throw new UnauthorizedAccessException("You do not have access to this loan's messages.");

            var paged = await _loanMessageRepository.GetByLoanIdAsync(loanId, request);

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
        }

        public async Task<int> GetUnreadCountAsync(int loanId, string userId)
        {
            var loan = await _loanRepository.GetByIdAsync(loanId)
                ?? throw new KeyNotFoundException("Loan not found.");

            if (loan.BorrowerId != userId && loan.LenderId != userId)
                throw new UnauthorizedAccessException("You do not have access to this loan.");

            return await _loanMessageRepository.GetUnreadCountAsync(loanId, userId);
        }


        //Helpers
        private static void EnsureMessagingAllowed(Loan loan, string senderId, bool isAdmin)
        {
            //Hard locked statuses — no one can message
            if (loan.Status == LoanStatus.Rejected || loan.Status == LoanStatus.Cancelled)
                throw new InvalidOperationException("Messaging is not available for rejected or cancelled loans.");

            //Locked 7 days after completion
            if (loan.Status == LoanStatus.Completed && loan.ActualReturnDate.HasValue)
            {
                var lockDate = loan.ActualReturnDate.Value.AddDays(LockDaysAfterCompletion);
                if (DateTime.UtcNow >= lockDate)
                    throw new InvalidOperationException("The message thread for this loan has been locked.");
            }

            if (isAdmin)
            {
                //Admin can ONLY message during AdminPending
                if (loan.Status != LoanStatus.AdminPending)
                    throw new InvalidOperationException("Admins can only send messages while the loan is pending admin review.");

                return;
            }

            // Borrower and lender cannot message during AdminPending — that's admin's conversation
            if (loan.Status == LoanStatus.AdminPending)
                throw new InvalidOperationException("Messaging is not available while the loan is pending admin review.");

            //Borrower and lender only
            if (loan.BorrowerId != senderId && loan.LenderId != senderId)
                throw new UnauthorizedAccessException("Only the borrower and lender can send messages on this loan.");
        }

        public async Task<bool> IsPartyToLoanAsync(int loanId, string userId)
        {
            var loan = await _loanRepository.GetByIdAsync(loanId);
            return loan != null && (loan.BorrowerId == userId || loan.LenderId == userId);
        }


    }
}