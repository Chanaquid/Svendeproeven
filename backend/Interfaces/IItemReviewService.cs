using backend.Dtos;

namespace backend.Interfaces
{
    public interface IItemReviewService
    {

        Task<ItemReviewDto> CreateItemReviewAsync(string reviewerId,
            CreateItemReviewDto dto);
        Task<ItemReviewDto> EditItemReviewAsync(int reviewId, string currentUserId,
            UpdateItemReviewDto dto);
        Task DeleteItemReviewAsync(int reviewId, string adminId);

        Task<PagedResult<ItemReviewDto>> GetByItemIdAsync(
            int itemId,
            string? currentUserId,
            ItemReviewFilter? filter,
            PagedRequest request);

    }
}