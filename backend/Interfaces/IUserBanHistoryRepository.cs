using backend.Dtos;
using backend.Models;

namespace backend.Interfaces
{
    //Logs
    public interface IUserBanHistoryRepository
    {
        Task<UserBanHistory?> GetByIdAsync(int id);
        Task<PagedResult<UserBanHistory>> GetByUserIdAsync(string userId, UserBanHistoryFilter? filter, PagedRequest request);
        Task<PagedResult<UserBanHistory>> GetAllAsync(UserBanHistoryFilter? filter, PagedRequest request);
        Task<UserBanHistory?> GetLatestByUserIdAsync(string userId);
        Task AddAsync(UserBanHistory banHistory);
        Task SaveChangesAsync();
    }
}