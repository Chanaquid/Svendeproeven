using backend.Dtos;
using backend.Models;
using System.Threading.Tasks;

namespace backend.Interfaces
{
    public interface IDisputeRepository
    {

        Task<int> CountAsync(DisputeFilter filter);
        Task<double> GetAverageResolutionDaysAsync();


        //Basic CRUD
        Task<Dispute?> GetByIdAsync(int disputeId);
        Task<Dispute?> GetByIdWithDetailsAsync(int disputeId); //Includes Loan, FiledBy, RespondedBy, Photos
        Task AddAsync(Dispute dispute);
        void Update(Dispute dispute);
        Task SaveChangesAsync();


        //loan-specific queries
        Task<List<Dispute>> GetByLoanIdAsync(int loanId);//Max 2, for admin loan view
        Task<Dispute?> GetActiveDisputeByLoanIdAsync(int loanId); //awaitingresponse | pendingaDminReview | PastDeadline
        Task<bool> HasActiveDisputeAsync(int loanId);

        Task<bool> HasActiveDisputeByUserIdAsync(string userId);
        Task<bool> HasUserFiledDisputeForLoanAsync(int loanId, string userId);
        Task<int> GetDisputeCountForLoanAsync(int loanId);


        //User - list view
        Task<PagedResult<Dispute>> GetFiledByUserAsync(string userId, DisputeFilter? filter, PagedRequest request);
        Task<PagedResult<Dispute>> GetRespondedToByUserAsync(string userId, DisputeFilter? filter, PagedRequest request);
        Task<PagedResult<Dispute>> GetAllDisputesByUserIdAsync(string userId, DisputeFilter? filter, PagedRequest request);


        //Admin
        Task<PagedResult<Dispute>> GetAllAsync(DisputeFilter? filter, PagedRequest request);
        Task<PagedResult<Dispute>> GetAllOpenAsync(DisputeFilter? filter, PagedRequest request); //AwaitingResponse + PendingAdminReview + PastDeadline
        Task<PagedResult<Dispute>> GetByStatusAsync(DisputeStatus status, DisputeFilter? filter, PagedRequest request);
        Task<PagedResult<Dispute>> GetDisputeHistoryByItemIdAsync(int itemId, DisputeFilter? filter, PagedRequest request);

        //BG task
        Task<List<Dispute>> GetExpiredAwaitingResponseAsync(); //ResponseDeadline < UtcNow && Status == AwaitingResponse


        //Statistics
        Task<int> GetOpenCountAsync();
        Task<int> GetPastDeadlineCountAsync();
        Task<int> GetResolvedCountByMonthAsync(int year, int month);
        Task<Dictionary<DisputeStatus, int>> GetStatusCountsAsync();
        Task<Dictionary<DisputeVerdict, int>> GetVerdictCountsAsync();


        //Photos
        Task AddPhotoAsync(DisputePhoto photo);
        Task<DisputePhoto?> GetPhotoByIdAsync(int photoId);
        Task<List<DisputePhoto>> GetPhotosByDisputeIdAsync(int disputeId);
        Task DeletePhotoAsync(DisputePhoto photo);


    }
}
