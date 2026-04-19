using backend.Dtos;
using backend.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace backend.Interfaces
{
    public interface ILoanRepository
    {
        Task<Loan?> GetByIdAsync(int loanId);
        Task<Loan?> GetByIdWithDetailsAsync(int loanId);
        Task<Loan?> GetActiveLoanByItemIdAsync(int itemId);

        //List - used by background jobs and other method checks
        Task<List<Loan>> GetByBorrowerIdAsync(string borrowerId);
        Task<List<Loan>> GetByOwnerIdAsync(string ownerId);
        Task<List<Loan>> GetAllAsync();
        Task<List<Loan>> GetPendingAdminApprovalsAsync();
        Task<List<Loan>> GetActiveAndOverdueAsync();
        Task<List<Loan>> GetByStatusAsync(LoanStatus status);
        Task<List<Loan>> GetLoanHistoryByItemIdAsync(int itemId);
        Task<List<Loan>> GetExpiredPendingLoansAsync(DateTime cutoff);
        Task<List<Loan>> GetOverdueActiveLoansAsync();

        Task<int> GetAllCompletedLoansCountByUserIdAsync(string userId);

        //Paged - for UI
        Task<PagedResult<Loan>> GetByBorrowerIdPagedAsync(string borrowerId, LoanFilter? filter, PagedRequest request);
        Task<PagedResult<Loan>> GetByOwnerIdPagedAsync(string ownerId, LoanFilter? filter, PagedRequest request);
        Task<PagedResult<Loan>> GetAllAsAdminAsync(LoanFilter? filter, PagedRequest request);

        //Checks
        Task<bool> HasOngoingLoansAsBorrower(string userId);
        Task<bool> HasOngoingLoansAsOwner(string userId);
        Task<bool> HasOverlappingLoanAsync(int itemId, DateTime startDate, DateTime endDate);
        Task<bool> HasActiveOrApprovedLoansByItemIdAsync(int itemId);
        Task<bool> IsItemAvailableForDatesAsync(int itemId, DateTime startDate, DateTime endDate);
        Task<bool> ExistsAsync(int loanId);

        //Counts — admin dashboard
        Task<int> GetPendingAdminApprovalsCountAsync();
        Task<int> GetActiveLoansCountAsync();

        Task AddAsync(Loan loan);
        void Update(Loan loan);
        Task SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}