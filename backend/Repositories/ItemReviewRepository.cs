using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class ItemReviewRepository : IItemReviewRepository
    {
        private readonly ApplicationDbContext _context;

        public ItemReviewRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ItemReview?> GetItemReviewByLoanIdAsync(int loanId)
        {
            return await _context.ItemReviews
                .Include(r => r.Reviewer)
                .FirstOrDefaultAsync(r => r.LoanId == loanId);
        }

        //Get single review by its ID
        public async Task<ItemReview?> GetItemReviewByIdAsync(int reviewId)
        {
            return await _context.ItemReviews
                .Include(r => r.Reviewer)
                .FirstOrDefaultAsync(r => r.Id == reviewId);
        }


        public async Task<PagedResult<ItemReview>> GetItemReviewsByItemIdAsync(
            int itemId,
            ItemReviewFilter? filter,
            PagedRequest request)
        {
            var query = _context.ItemReviews
                .AsNoTracking()
                .Include(r => r.Reviewer)
                .Where(r => r.ItemId == itemId)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = ApplyDefaultSort(query, request);

            return await query.ToPagedResultAsync(request);
        }

        public async Task AddItemReviewAsync(ItemReview review)
        {
            await _context.ItemReviews.AddAsync(review);
        }

        public void Update(ItemReview review)
        {
            _context.Entry(review).State = EntityState.Modified;
        }
        public void MarkReviewsDeletedByItemId(int itemId) //Called by Itemsoftdelete method
        {
            _context.ItemReviews
                .Where(r => r.ItemId == itemId)
                .ExecuteUpdate(setters => setters
                    .SetProperty(r => r.IsDeleted, true)
                    .SetProperty(r => r.DeletedAt, DateTime.UtcNow));
        }

        //Helper - ensure reviewer navigation is loaded when needed
        public async Task LoadReviewerAsync(ItemReview review)
        {
            await _context.Entry(review)
                .Reference(r => r.Reviewer)
                .LoadAsync();
        }


        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }


        //Filtering
        private static IQueryable<ItemReview> ApplyFilter(
          IQueryable<ItemReview> query,
          ItemReviewFilter? filter)
        {
            if (filter == null)
                return query;

            if (filter.MinRating.HasValue)
                query = query.Where(r => r.Rating >= filter.MinRating.Value);

            if (filter.MaxRating.HasValue)
                query = query.Where(r => r.Rating <= filter.MaxRating.Value);

            if (filter.IsVerifiedReviewer.HasValue)
                query = query.Where(r => r.Reviewer.IsVerified == filter.IsVerifiedReviewer.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();

                query = query.Where(r =>
                    r.Comment != null &&
                    r.Comment.ToLower().Contains(term));
            }

            if (filter.FromDate.HasValue)
                query = query.Where(r => r.CreatedAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(r => r.CreatedAt <= filter.ToDate.Value);

            if (filter.HasComment.HasValue)
            {
                if (filter.HasComment.Value)
                    query = query.Where(r => !string.IsNullOrWhiteSpace(r.Comment));
                else
                    query = query.Where(r => string.IsNullOrWhiteSpace(r.Comment));
            }

            return query;
        }

        //Sorting
        private static IQueryable<ItemReview> ApplyDefaultSort(
            IQueryable<ItemReview> query,
            PagedRequest request)
        {
            return string.IsNullOrWhiteSpace(request.SortBy)
                ? query.OrderByDescending(r => r.CreatedAt)
                : query.ApplySorting(request.SortBy, request.SortDescending);
        }


    }
}
