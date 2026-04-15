using backend.Dtos;
using backend.Models;

namespace backend.Interfaces
{
    public interface IUserReviewRepository
    {
        Task<UserReview?> GetByIdAsync(int id);
        Task<UserReview?> GetByIdWithDetailsAsync(int id);
        Task<PagedResult<UserReview>> GetByReviewedUserIdAsync(string reviewedUserId, UserReviewFilter? filter, PagedRequest request);
        Task<PagedResult<UserReview>> GetByReviewerIdAsync(string reviewerId, UserReviewFilter? filter, PagedRequest request);
        Task<PagedResult<UserReview>> GetAllAsync(UserReviewFilter? filter, PagedRequest request);
        Task<UserRatingSummaryDto> GetRatingSummaryAsync(string reviewedUserId);
        Task<UserReview?> GetByLoanAndReviewerAsync(int loanId, string reviewerId);
        Task<bool> HasReviewedUserAsync(string reviewerId, string reviewedUserId);
        Task<bool> HasCompletedLoanTogetherAsync(string userId1, string userId2);
        Task AddAsync(UserReview review);
        void Update(UserReview review);
        Task SaveChangesAsync();
    }
}