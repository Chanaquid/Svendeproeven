using backend.Dtos;
using backend.Models;

namespace backend.Interfaces
{
    public interface IUserFavoriteRepository
    {
        Task<PagedResult<UserFavoriteItem>> GetAllByUserIdAsync(string userId, PagedRequest request);
        Task<UserFavoriteItem?> GetAsync(string userId, int itemId);
        Task<bool> ExistsAsync(string userId, int itemId);
        Task AddAsync(UserFavoriteItem favorite);

        Task<List<string>> GetUsersToNotifyAsync(int itemId);
        Task<List<UserFavoriteItem>> GetAllByItemIdAsync(int itemId); //For deletion cleanup
        void Remove(UserFavoriteItem favorite);
        void RemoveRange(List<UserFavoriteItem> favorites); //For bulk deletion cleanup
        Task SaveChangesAsync();
    }
}
