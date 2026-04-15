using backend.Data;
using backend.Interfaces;
using backend.Models;
using backend.Extensions;
using Microsoft.EntityFrameworkCore;
using backend.Dtos;

namespace backend.Repositories
{
    public class AppealRepository : IAppealRepository
    {
        private readonly ApplicationDbContext _context;

        public AppealRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Appeal?> GetByIdAsync(int appealId)
        {
            return await _context.Appeals.FindAsync(appealId);
        }

        //With details
        public async Task<Appeal?> GetByIdWithDetailsAsync(int appealId)
        {
            return await _context.Appeals
                .Include(a => a.User)
                .Include(a => a.Fine)
                .Include(a => a.ScoreHistory)
                .FirstOrDefaultAsync(a => a.Id == appealId);
        }

        public async Task<PagedResult<Appeal>> GetPendingByUserIdAsync(string userId,
            AppealFilter? filter,
            PagedRequest request)
        {

            filter ??= new AppealFilter();
            filter.UserId = userId;
            filter.Status = AppealStatus.Pending;
            var query = _context.Appeals.AsNoTracking().AsQueryable();
            return await ApplyFilterAndPaging(query, filter, request, includeDetails: true);
        }

        //Get all appeals - admin
        public async Task<PagedResult<Appeal>> GetAllAsync(AppealFilter? filter, PagedRequest request)
        {
            filter ??= new AppealFilter();
            var query = _context.Appeals.AsNoTracking().AsQueryable();
            return await ApplyFilterAndPaging(query, filter, request, includeDetails: false);
        }

        //Get all pending appeals - admin
        public async Task<PagedResult<Appeal>> GetAllPendingAsync(AppealFilter? filter, PagedRequest request)
        {
            filter ??= new AppealFilter();
            filter.Status = AppealStatus.Pending;
            var query = _context.Appeals.AsNoTracking().AsQueryable();
            return await ApplyFilterAndPaging(query, filter, request);
        }

        public async Task<PagedResult<Appeal>> GetAllByUserIdAsync(string userId, AppealFilter? filter, PagedRequest request)
        {
            filter ??= new AppealFilter();
            filter.UserId = userId;
            var query = _context.Appeals.AsNoTracking().AsQueryable();
            return await ApplyFilterAndPaging(query, filter, request, includeDetails: true);
        }

        //get the pending fine appeal by fine id
        public async Task<Appeal?> GetPendingFineAppealByFineIdAsync(int fineId)
        {
            return await _context.Appeals
                .FirstOrDefaultAsync(a => a.FineId == fineId && a.Status == AppealStatus.Pending);
        }

        public async Task<Appeal?> GetByScoreHistoryIdAsync(int scoreHistoryId)
        {
            return await _context.Appeals
                .FirstOrDefaultAsync(a => a.ScoreHistoryId == scoreHistoryId);
        }
        public async Task<PagedResult<Appeal>> GetAllByStatusAsync(AppealStatus status, AppealFilter? filter, PagedRequest request)
        {
            filter ??= new AppealFilter();
            filter.Status = status; 
            var query = _context.Appeals.AsNoTracking().AsQueryable();
            return await ApplyFilterAndPaging(query, filter, request, includeDetails: false);
        }


        public async Task<int> GetPendingCountAsync()
        {
            return await _context.Appeals
                .CountAsync(a => a.Status == AppealStatus.Pending);
        }

        public Task DeleteAsync(Appeal appeal)
        {
            _context.Appeals.Remove(appeal);
            return Task.CompletedTask;
        }

        public async Task AddAsync(Appeal appeal)
        {
            await _context.Appeals.AddAsync(appeal);
        }

        public void Update(Appeal appeal)
        {
            _context.Appeals.Update(appeal);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        //Validators
        public async Task<bool> HasPendingScoreAppealAsync(string userId)
        {
            return await _context.Appeals
                .AsNoTracking()
                .AnyAsync(a => a.UserId == userId
                           && a.AppealType == AppealType.Score
                           && a.Status == AppealStatus.Pending);
        }

        public async Task<bool> HasFineAppealAsync(string userId, int fineId)
        {
            //Cancelled and rejected appeals block refiling 
            return await _context.Appeals
                .AnyAsync(a => a.UserId == userId &&
                               a.FineId == fineId);
        }


        //Helper
        private async Task<PagedResult<Appeal>> ApplyFilterAndPaging(IQueryable<Appeal> query,
            AppealFilter filter,
            PagedRequest request,
            bool includeDetails = false)
        {

            if (includeDetails)
            {
                query = query.Include(a => a.Fine)
                             .Include(a => a.ScoreHistory);
            }

            query = query.Include(a => a.User).Include(a => a.ResolvedByAdmin);

            //Filter by type and status
            if (filter.AppealType.HasValue)
                query = query.Where(a => a.AppealType == filter.AppealType.Value);

            if (filter.Status.HasValue)
                query = query.Where(a => a.Status == filter.Status.Value);


            //Logic for IsResolved Flag
            if (filter.IsResolved.HasValue && !filter.Status.HasValue)
            {
                query = filter.IsResolved.Value
                    ? query.Where(a => a.Status != AppealStatus.Pending)
                    : query.Where(a => a.Status == AppealStatus.Pending);
            }

            //Filter by related entities
            if (!string.IsNullOrEmpty(filter.UserId))
                query = query.Where(a => a.UserId == filter.UserId);

            if (!string.IsNullOrEmpty(filter.ResolvedByAdminId))
                query = query.Where(a => a.ResolvedByAdminId == filter.ResolvedByAdminId);

            if (filter.FineId.HasValue)
                query = query.Where(a => a.FineId == filter.FineId.Value);

            if (filter.ScoreHistoryId.HasValue)
                query = query.Where(a => a.ScoreHistoryId == filter.ScoreHistoryId.Value);

            //Date filters
            if (filter.CreatedAfter.HasValue)
                query = query.Where(a => a.CreatedAt >= filter.CreatedAfter.Value);

            if (filter.CreatedBefore.HasValue)
                query = query.Where(a => a.CreatedAt <= filter.CreatedBefore.Value);

            if (filter.ResolvedAfter.HasValue)
                query = query.Where(a => a.ResolvedAt.HasValue && a.ResolvedAt.Value >= filter.ResolvedAfter.Value);

            if (filter.ResolvedBefore.HasValue)
                query = query.Where(a => a.ResolvedAt.HasValue && a.ResolvedAt.Value <= filter.ResolvedBefore.Value);

            //Search (Including User details if search is provided)
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.ToLower();
                query = query.Where(a => a.Message.ToLower().Contains(search)
                                      || (a.AdminNote != null && a.AdminNote.ToLower().Contains(search))
                                      || a.User.FullName.ToLower().Contains(search));
            }

            //Sorting
            //query = request.SortBy?.ToLower() switch
            //{
            //    "createdat" => request.SortDescending ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt),
            //    "resolvedat" => request.SortDescending ? query.OrderByDescending(a => a.ResolvedAt) : query.OrderBy(a => a.ResolvedAt),
            //    "type" => request.SortDescending ? query.OrderByDescending(a => a.AppealType) : query.OrderBy(a => a.AppealType),
            //    "status" => request.SortDescending ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
            //    _ => query.OrderByDescending(a => a.CreatedAt)
            //};
            query = string.IsNullOrWhiteSpace(request.SortBy)
                ? query.OrderByDescending(a => a.CreatedAt)
                : query.ApplySorting(request.SortBy, request.SortDescending);


            //Pagination
            return await query.ToPagedResultAsync(request);
        }
    }
}