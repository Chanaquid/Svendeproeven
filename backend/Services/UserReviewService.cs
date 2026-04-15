using backend.Dtos;
using backend.Interfaces;
using backend.Models;

namespace backend.Services
{
    public class UserReviewService : IUserReviewService
    {
        private readonly IUserReviewRepository _reviewRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILoanRepository _loanRepository;

        public UserReviewService(
            IUserReviewRepository reviewRepository,
            IUserRepository userRepository,
            ILoanRepository loanRepository)
        {
            _reviewRepository = reviewRepository;
            _userRepository = userRepository;
            _loanRepository = loanRepository;
        }

        public async Task<UserReviewDto> CreateReviewAsync(string reviewerId, CreateUserReviewDto dto)
        {
            var loan = await _loanRepository.GetByIdAsync(dto.LoanId)
                ?? throw new KeyNotFoundException("Loan not found.");

            if (loan.Status != LoanStatus.Completed)
                throw new InvalidOperationException("You can only leave a review after the loan is completed.");

            //Reviewer must be either the borrower or lender on this loan
            if (loan.BorrowerId != reviewerId && loan.LenderId != reviewerId)
                throw new UnauthorizedAccessException("You were not part of this loan.");

            //ReviewedUser is the other party on the loan
            var reviewedUserId = loan.BorrowerId == reviewerId
                ? loan.LenderId
                : loan.BorrowerId;

            //One review per user - regardless of loans
            if (await _reviewRepository.HasReviewedUserAsync(reviewerId, reviewedUserId))
                throw new InvalidOperationException("You have already reviewed this user.");

            var review = new UserReview
            {
                LoanId = dto.LoanId,
                ReviewerId = reviewerId,
                ReviewedUserId = reviewedUserId,
                Rating = dto.Rating,
                Comment = dto.Comment?.Trim(),
                IsAdminReview = false,
                CreatedAt = DateTime.UtcNow
            };

            await _reviewRepository.AddAsync(review);
            await _reviewRepository.SaveChangesAsync();

            var created = await _reviewRepository.GetByIdWithDetailsAsync(review.Id);
            return MapToDto(created!, reviewerId);
        }

        public async Task<UserReviewDto> UpdateReviewAsync(int reviewId, string reviewerId, UpdateUserReviewDto dto)
        {
            var review = await _reviewRepository.GetByIdWithDetailsAsync(reviewId)
                ?? throw new KeyNotFoundException("Review not found.");

            if (review.ReviewerId != reviewerId)
                throw new UnauthorizedAccessException("You can only edit your own reviews.");

            if (review.IsAdminReview)
                throw new InvalidOperationException("Admin reviews cannot be edited this way.");

            review.Rating = dto.Rating;
            review.Comment = dto.Comment?.Trim();
            review.IsEdited = true;
            review.EditedAt = DateTime.UtcNow;

            _reviewRepository.Update(review);
            await _reviewRepository.SaveChangesAsync();

            return MapToDto(review, reviewerId);
        }

        public async Task<UserReviewDto> GetByIdAsync(int id, string currentUserId, bool isAdmin)
        {
            var review = await _reviewRepository.GetByIdWithDetailsAsync(id)
                ?? throw new KeyNotFoundException("Review not found.");

            return MapToDto(review, currentUserId);
        }

        public async Task<PagedResult<UserReviewListDto>> GetReviewsForUserAsync(
            string reviewedUserId,
            UserReviewFilter? filter,
            PagedRequest request)
        {
            var paged = await _reviewRepository.GetByReviewedUserIdAsync(reviewedUserId, filter, request);
            return MapPagedResultToListDto(paged, currentUserId: null);
        }

        public async Task<PagedResult<UserReviewListDto>> GetMyGivenReviewsAsync(
            string reviewerId,
            UserReviewFilter? filter,
            PagedRequest request)
        {
            var paged = await _reviewRepository.GetByReviewerIdAsync(reviewerId, filter, request);
            return MapPagedResultToListDto(paged, reviewerId);
        }

        public async Task<UserRatingSummaryDto> GetRatingSummaryAsync(string reviewedUserId)
        {
            return await _reviewRepository.GetRatingSummaryAsync(reviewedUserId);
        }

        //Admin

        public async Task<UserReviewDto> AdminCreateReviewAsync(string adminId, AdminCreateUserReviewDto dto)
        {
            _ = await _userRepository.GetByIdAsync(dto.ReviewedUserId)
                ?? throw new KeyNotFoundException("Reviewed user not found.");

            //Admin reviews have no loan requirement and no duplicate check
            var review = new UserReview
            {
                LoanId = null,
                ReviewerId = adminId,
                ReviewedUserId = dto.ReviewedUserId,
                Rating = dto.Rating,
                Comment = dto.Comment.Trim(),
                IsAdminReview = true,
                CreatedAt = DateTime.UtcNow
            };

            await _reviewRepository.AddAsync(review);
            await _reviewRepository.SaveChangesAsync();

            var created = await _reviewRepository.GetByIdWithDetailsAsync(review.Id);
            return MapToDto(created!, adminId);
        }

        //Users cant delete reviews
        public async Task AdminDeleteReviewAsync(int reviewId)
        {
            var review = await _reviewRepository.GetByIdAsync(reviewId)
                ?? throw new KeyNotFoundException("Review not found.");

            review.IsDeleted = true;
            review.DeletedAt = DateTime.UtcNow;
            _reviewRepository.Update(review);
            await _reviewRepository.SaveChangesAsync();
        }

        public async Task<PagedResult<UserReviewDto>> GetAllAsync(
            UserReviewFilter? filter,
            PagedRequest request)
        {
            var paged = await _reviewRepository.GetAllAsync(filter, request);

            return new PagedResult<UserReviewDto>
            {
                Items = paged.Items.Select(r => MapToDto(r, currentUserId: null)).ToList(),
                TotalCount = paged.TotalCount,
                Page = paged.Page,
                PageSize = paged.PageSize
            };
        }

        //helper

        private static UserReviewDto MapToDto(UserReview r, string? currentUserId)
        {
            return new UserReviewDto
            {
                Id = r.Id,
                LoanId = r.LoanId,
                ReviewerId = r.ReviewerId,
                ReviewerName = r.Reviewer?.FullName ?? string.Empty,
                ReviewerUserName = r.Reviewer?.UserName ?? string.Empty,
                ReviewerAvatarUrl = r.Reviewer?.AvatarUrl,
                ReviewedUserId = r.ReviewedUserId,
                ReviewedFullName = r.ReviewedUser?.FullName ?? string.Empty,
                ReviewedUserName = r.ReviewedUser?.UserName ?? string.Empty,
                ReviewedUserAvatarUrl = r.ReviewedUser?.AvatarUrl,
                IsMine = currentUserId != null && r.ReviewerId == currentUserId,
                IsAdminReview = r.IsAdminReview,
                IsEdited = r.IsEdited,
                EditedAt = r.EditedAt,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            };
        }

        private static PagedResult<UserReviewListDto> MapPagedResultToListDto(
            PagedResult<UserReview> source,
            string? currentUserId)
        {
            return new PagedResult<UserReviewListDto>
            {
                Items = source.Items.Select(r => new UserReviewListDto
                {
                    Id = r.Id,
                    ReviewerName = r.Reviewer?.FullName ?? string.Empty,
                    ReviewerUserName = r.Reviewer?.UserName ?? string.Empty,
                    ReviewerAvatarUrl = r.Reviewer?.AvatarUrl,
                    IsAdminReview = r.IsAdminReview,
                    IsMine = currentUserId != null && r.ReviewerId == currentUserId,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                }).ToList(),
                TotalCount = source.TotalCount,
                Page = source.Page,
                PageSize = source.PageSize
            };
        }
    }
}