using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class UserReviewRepository : IUserReviewRepository
    {
        private readonly ApplicationDbContext _context;

        public UserReviewRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserReview?> GetByIdAsync(int id)
        {
            return await _context.UserReviews.FindAsync(id);
        }

        public async Task<UserReview?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.UserReviews
                .Include(r => r.Reviewer)
                .Include(r => r.ReviewedUser)
                .Include(r => r.Loan)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<PagedResult<UserReview>> GetByReviewedUserIdAsync(
            string reviewedUserId,
            UserReviewFilter? filter,
            PagedRequest request)
        {
            var query = _context.UserReviews
                .AsNoTracking()
                .Include(r => r.Loan).ThenInclude(l => l!.Item)
                .Include(r => r.Reviewer)
                .Include(r => r.ReviewedUser)
                .Where(r => r.ReviewedUserId == reviewedUserId)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = ApplySorting(query, request);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<PagedResult<UserReview>> GetByReviewerIdAsync(
            string reviewerId,
            UserReviewFilter? filter,
            PagedRequest request)
        {
            var query = _context.UserReviews
                .AsNoTracking()
                .Include(r => r.Reviewer)
                .Include(r => r.ReviewedUser)
                .Include(r => r.Loan)
                    .ThenInclude(l => l!.Item)

                .Where(r => r.ReviewerId == reviewerId)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = ApplySorting(query, request);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<PagedResult<UserReview>> GetAllAsync(
            UserReviewFilter? filter,
            PagedRequest request)
        {
            var query = _context.UserReviews
                .AsNoTracking()
                .Include(r => r.Reviewer)
                .Include(r => r.ReviewedUser)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = ApplySorting(query, request);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<UserRatingSummaryDto> GetRatingSummaryAsync(string reviewedUserId)
        {
            var ratings = await _context.UserReviews
                .AsNoTracking()
                .Where(r => r.ReviewedUserId == reviewedUserId)
                .Select(r => r.Rating)
                .ToListAsync();

            if (ratings.Count == 0)
                return new UserRatingSummaryDto();

            return new UserRatingSummaryDto
            {
                AverageRating = Math.Round(ratings.Average(), 2),
                TotalReviews = ratings.Count,
                Rating1Count = ratings.Count(r => r == 1),
                Rating2Count = ratings.Count(r => r == 2),
                Rating3Count = ratings.Count(r => r == 3),
                Rating4Count = ratings.Count(r => r == 4),
                Rating5Count = ratings.Count(r => r == 5)
            };
        }

        public async Task<UserReview?> GetByLoanAndReviewerAsync(int loanId, string reviewerId)
        {
            return await _context.UserReviews
                .FirstOrDefaultAsync(r => r.LoanId == loanId && r.ReviewerId == reviewerId);
        }

        public async Task<bool> HasReviewedUserAsync(string reviewerId, string reviewedUserId)
        {
            return await _context.UserReviews
                .AnyAsync(r => r.ReviewerId == reviewerId && r.ReviewedUserId == reviewedUserId);
        }

        public async Task<bool> HasCompletedLoanTogetherAsync(string userId1, string userId2)
        {
            //Verification check to ensure reviews only happen between legitimate transaction partners
            return await _context.Loans
                .AnyAsync(l =>
                    l.Status == LoanStatus.Completed &&
                    (
                        (l.BorrowerId == userId1 && l.LenderId == userId2) ||
                        (l.BorrowerId == userId2 && l.LenderId == userId1)
                    ));
        }

        public async Task AddAsync(UserReview review)
        {
            await _context.UserReviews.AddAsync(review);
        }

        public void Update(UserReview review)
        {
            _context.UserReviews.Update(review);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        //helpers
        private static IQueryable<UserReview> ApplyFilter(IQueryable<UserReview> query, UserReviewFilter? filter)
        {
            if (filter == null) return query;

            if (!string.IsNullOrWhiteSpace(filter.ReviewerId))
                query = query.Where(r => r.ReviewerId == filter.ReviewerId);

            if (!string.IsNullOrWhiteSpace(filter.ReviewedUserId))
                query = query.Where(r => r.ReviewedUserId == filter.ReviewedUserId);

            if (filter.LoanId.HasValue)
                query = query.Where(r => r.LoanId == filter.LoanId.Value);

            if (filter.MinRating.HasValue)
                query = query.Where(r => r.Rating >= filter.MinRating.Value);

            if (filter.MaxRating.HasValue)
                query = query.Where(r => r.Rating <= filter.MaxRating.Value);

            if (filter.IsAdminReview.HasValue)
                query = query.Where(r => r.IsAdminReview == filter.IsAdminReview.Value);

            if (filter.IsEdited.HasValue)
                query = query.Where(r => r.IsEdited == filter.IsEdited.Value);

            if (filter.CreatedAfter.HasValue)
                query = query.Where(r => r.CreatedAt >= filter.CreatedAfter.Value);

            if (filter.CreatedBefore.HasValue)
                query = query.Where(r => r.CreatedAt <= filter.CreatedBefore.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.ToLower();
                query = query.Where(r => r.Comment != null && r.Comment.ToLower().Contains(search));
            }

            return query;
        }

        private static IQueryable<UserReview> ApplySorting(IQueryable<UserReview> query, PagedRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.SortBy))
                return query.ApplySorting(request.SortBy, request.SortDescending);

            return query.OrderByDescending(r => r.CreatedAt);
        }
    }
}