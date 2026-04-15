using backend.Dtos;
using backend.Models;

namespace backend.Interfaces
{
    public interface IUserBlockRepository
    {
        Task<UserBlock?> GetAsync(string blockerId, string blockedId);
        Task<PagedResult<UserBlock>> GetBlocksByBlockerAsync(string blockerId, UserBlockFilter? filter, PagedRequest request);
        Task<PagedResult<UserBlock>> GetAllAsync(UserBlockFilter? filter, PagedRequest request); //admin
        Task<bool> IsBlockedAsync(string blockerId, string blockedId);
        Task<bool> AreBlockedEitherWayAsync(string userId1, string userId2);
        Task<HashSet<string>> GetBlockedUserIdsAsync(string userId); //all users blocked by or blocking userId
        Task AddAsync(UserBlock block);
        Task DeleteAsync(UserBlock block);
        Task SaveChangesAsync();
    }
}