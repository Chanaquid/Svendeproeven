using backend.Dtos;

namespace backend.Interfaces
{
    public interface ILoanMessageService
    {
        Task<LoanMessageDto> SendMessageAsync(int loanId, string senderId, SendLoanMessageDto dto, bool isAdmin);
        Task<PagedResult<LoanMessageDto>> GetMessagesAsync(int loanId, string userId, bool isAdmin, PagedRequest request);
        Task MarkAsReadAsync(int loanId, string userId, MarkLoanMessagesReadDto dto);
        Task<int> GetUnreadCountAsync(int loanId, string userId);
        Task<bool> IsPartyToLoanAsync(int loanId, string userId);
    }
}