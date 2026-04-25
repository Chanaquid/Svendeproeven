using backend.Dtos;
using backend.Models;

namespace backend.Interfaces
{
    public interface IFineRepository
    {
        Task<int> CountAsync(FineFilter filter);
        Task<decimal> SumAmountAsync(FineFilter filter);

        Task<Fine?> GetByIdAsync(int fineId);
        Task<Fine?> GetByIdWithDetailsAsync(int fineId);

        Task AddAsync(Fine fine);
        void Update(Fine fine);
        Task SaveChangesAsync();

        Task<bool> ExistsPaidFineAsync(string userId, int? loanId, int? disputeId);
        Task<bool> ExistsActiveFineAsync(string userId, int? loanId, int? disputeId);

        //User 
        Task<PagedResult<Fine>> GetByUserIdAsync(string userId, FineFilter? filter, PagedRequest request);

        //Admin 
        Task<PagedResult<Fine>> GetAllAsync(FineFilter? filter, PagedRequest request);
        Task<PagedResult<Fine>> GetByStatusAsync(FineStatus status, FineFilter? filter, PagedRequest request);
        Task<PagedResult<Fine>> GetPendingProofReviewAsync(FineFilter? filter, PagedRequest request);

        //Loan / dispute scoped
        Task<List<Fine>> GetByLoanIdAsync(int loanId);
        Task<List<Fine>> GetByDisputeIdAsync(int disputeId);

        //Stats
        Task<Dictionary<FineStatus, int>> GetStatusCountsAsync();
        Task<Dictionary<FineType, int>> GetTypeCountsAsync();
        Task<int> GetIssuedThisMonthCountAsync();
        Task<int> GetPendingProofCountAsync();

        //Outstanding fines
        Task<Dictionary<string, decimal>> GetOutstandingTotalsByUsersAsync(List<string> userIds);
        Task<bool> HasOutstandingFinesAsync(string userId);
        Task<decimal> GetOutstandingTotalByUserAsync(string userId);
        Task<decimal> GetOutstandingTotalAsync();
    }

}





