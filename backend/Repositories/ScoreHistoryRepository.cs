using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class ScoreHistoryRepository : IScoreHistoryRepository
    {
        private readonly ApplicationDbContext _context;

        public ScoreHistoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ScoreHistory?> GetByIdAsync(int id)
        {
            return await _context.ScoreHistories.FindAsync(id);
        }

        public async Task<PagedResult<ScoreHistory>> GetByUserIdAsync(string userId, ScoreHistoryFilter? filter, PagedRequest request)
        {
            var query = _context.ScoreHistories
                .AsNoTracking()
                .Where(s => s.UserId == userId);

            query = ApplyFilter(query, filter);
            query = ApplySorting(query, request);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<PagedResult<ScoreHistory>> GetAllAsync(ScoreHistoryFilter? filter, PagedRequest request)
        {
            var query = _context.ScoreHistories
                .AsNoTracking()
                .Include(s => s.User);

            var filtered = ApplyFilter(query, filter);
            var sorted = ApplySorting(filtered, request);

            return await sorted.ToPagedResultAsync(request);
        }

        public async Task<UserScoreSummaryDto> GetScoreSummaryByUserIdAsync(string userId)
        {
            var history = await _context.ScoreHistories
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .ToListAsync();

            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.Score })
                .FirstOrDefaultAsync();

            return new UserScoreSummaryDto
            {
                CurrentScore = user?.Score ?? 0,
                TotalPointsEarned = history.Where(s => s.PointsChanged > 0).Sum(s => s.PointsChanged),
                TotalPointsLost = history.Where(s => s.PointsChanged < 0).Sum(s => s.PointsChanged),
                TotalScoreEvents = history.Count,
                LastScoreChangeAt = history.OrderByDescending(s => s.CreatedAt).FirstOrDefault()?.CreatedAt
            };
        }

        //Used by other services
        public async Task<List<ScoreHistory>> GetScoreHistoryByUserIdAsync(string userId)
        {
            return await _context.ScoreHistories
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task AddAsync(ScoreHistory scoreHistory)
        {
            await _context.ScoreHistories.AddAsync(scoreHistory);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        //Helpers
        private static IQueryable<ScoreHistory> ApplyFilter(IQueryable<ScoreHistory> query, ScoreHistoryFilter? filter)
        {
            if (filter == null) return query;

            if (!string.IsNullOrWhiteSpace(filter.UserId))
                query = query.Where(s => s.UserId == filter.UserId);

            if (filter.Reason.HasValue)
                query = query.Where(s => s.Reason == filter.Reason.Value);

            if (filter.LoanId.HasValue)
                query = query.Where(s => s.LoanId == filter.LoanId.Value);

            if (filter.DisputeId.HasValue)
                query = query.Where(s => s.DisputeId == filter.DisputeId.Value);

            if (filter.CreatedAfter.HasValue)
                query = query.Where(s => s.CreatedAt >= filter.CreatedAfter.Value);

            if (filter.CreatedBefore.HasValue)
                query = query.Where(s => s.CreatedAt <= filter.CreatedBefore.Value);

            if (filter.OnlyPositive == true)
                query = query.Where(s => s.PointsChanged > 0);

            if (filter.OnlyNegative == true)
                query = query.Where(s => s.PointsChanged < 0);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.ToLower();
                query = query.Where(s => s.Note != null && s.Note.ToLower().Contains(search));
            }

            return query;
        }
        private static IQueryable<ScoreHistory> ApplySorting(IQueryable<ScoreHistory> query, PagedRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.SortBy))
                return query.ApplySorting(request.SortBy, request.SortDescending);

            return query.OrderByDescending(s => s.CreatedAt);
        }
    }
}