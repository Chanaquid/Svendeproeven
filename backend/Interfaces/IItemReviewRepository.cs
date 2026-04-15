using backend.Dtos;
using backend.Models;

namespace backend.Interfaces
{
    public interface IItemReviewRepository
    {

        Task<ItemReview?> GetItemReviewByIdAsync(int reviewId);
        Task<ItemReview?> GetItemReviewByLoanIdAsync(int loanId);
        Task<PagedResult<ItemReview>> GetItemReviewsByItemIdAsync(int itemId, ItemReviewFilter? filter, PagedRequest request);
        Task AddItemReviewAsync(ItemReview review);
        Task LoadReviewerAsync(ItemReview review);


        void Update(ItemReview review);
        void MarkReviewsDeletedByItemId(int itemId); //Called when item is deleted

        Task SaveChangesAsync();
    }
}
