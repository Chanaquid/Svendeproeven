using backend.Dtos;

namespace backend.Interfaces
{
    public interface IUserRecentlyViewedService
    {
        Task TrackViewAsync(string userId, int itemId);
        Task<List<UserRecentlyViewedItemDto>> GetRecentlyViewedAsync(string userId);
    }
}