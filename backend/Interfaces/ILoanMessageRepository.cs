using backend.Dtos;
using backend.Models;

namespace backend.Interfaces
{
    public interface ILoanMessageRepository
    {
        Task<LoanMessage?> GetByIdAsync(int id);
        Task<PagedResult<LoanMessage>> GetByLoanIdAsync(int loanId, PagedRequest request);
        Task<LoanMessage?> GetLastMessageAsync(int loanId);
        Task<int> GetUnreadCountAsync(int loanId, string userId);
        Task<List<LoanMessage>> GetUnreadMessagesForUserAsync(int loanId, string userId);
        Task AddAsync(LoanMessage message);
        Task MarkAsReadAsync(int loanId, string userId, int? upToMessageId = null);
        Task SaveChangesAsync();
    }
}