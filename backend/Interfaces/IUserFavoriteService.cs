using backend.Dtos;

namespace backend.Interfaces
{
    public interface IUserFavoriteService
    {
        Task<PagedResult<ItemListDto>> GetFavoritesAsync(string userId, PagedRequest request);
        Task<bool> ToggleFavoriteAsync(string userId, int itemId, bool notify);
        Task<bool> IsFavoritedAsync(string userId, int itemId);
        Task UpdateNotifyPreferenceAsync(string userId, int itemId, bool notify);
    }
}
