using backend.Dtos;
using backend.Models;

namespace backend.Interfaces
{
    public interface IAppealRepository
    {
        Task<Appeal?> GetByIdAsync(int appealId);
        Task<Appeal?> GetByIdWithDetailsAsync(int appealId);

        Task<Appeal?> GetPendingFineAppealByFineIdAsync(int fineId);
        Task<Appeal?> GetByScoreHistoryIdAsync(int scoreHistoryId);
        Task<PagedResult<Appeal>> GetPendingByUserIdAsync(string userId, AppealFilter? filter, PagedRequest request);

        Task<PagedResult<Appeal>> GetAllAsync(AppealFilter? filter, PagedRequest request); //admin gets all appeals
        Task<PagedResult<Appeal>> GetAllByUserIdAsync(string userId, AppealFilter? filter, PagedRequest request);
        Task<PagedResult<Appeal>> GetAllByStatusAsync(AppealStatus status, AppealFilter? filter, PagedRequest request);


        Task<bool> HasPendingScoreAppealAsync(string userId);
        Task<bool> HasFineAppealAsync(string userId, int fineId);
       

        Task<int> GetPendingCountAsync();
        Task AddAsync(Appeal appeal);
        void Update(Appeal appeal);
        Task DeleteAsync(Appeal appeal);
        Task SaveChangesAsync();
    }
}