using backend.Models;

namespace backend.Interfaces
{
    public interface IUserRecentlyViewedRepository
    {
        Task<List<UserRecentlyViewedItem>> GetByUserIdAsync(string userId, int limit);
        Task<UserRecentlyViewedItem?> GetAsync(string userId, int itemId);
        Task AddAsync(UserRecentlyViewedItem item);
        Task<int> GetCountByUserIdAsync(string userId);
        Task DeleteOldestAsync(string userId);
        void Update(UserRecentlyViewedItem item);
        Task SaveChangesAsync();
    }
}