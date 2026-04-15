using backend.Dtos;

namespace backend.Interfaces
{
    public interface IUserReviewService
    {
        //User
        Task<UserReviewDto> CreateReviewAsync(string reviewerId, CreateUserReviewDto dto);
        Task<UserReviewDto> UpdateReviewAsync(int reviewId, string reviewerId, UpdateUserReviewDto dto);
        Task<PagedResult<UserReviewListDto>> GetReviewsForUserAsync(string reviewedUserId, UserReviewFilter? filter, PagedRequest request);
        Task<PagedResult<UserReviewListDto>> GetMyGivenReviewsAsync(string reviewerId, UserReviewFilter? filter, PagedRequest request);
        Task<UserRatingSummaryDto> GetRatingSummaryAsync(string reviewedUserId);
        Task<UserReviewDto> GetByIdAsync(int id, string currentUserId, bool isAdmin);

        //Admin
        Task<UserReviewDto> AdminCreateReviewAsync(string adminId, AdminCreateUserReviewDto dto);
        Task AdminDeleteReviewAsync(int reviewId);
        Task<PagedResult<UserReviewDto>> GetAllAsync(UserReviewFilter? filter, PagedRequest request);
    }
}