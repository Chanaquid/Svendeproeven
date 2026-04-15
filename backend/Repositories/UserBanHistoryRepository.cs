using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class UserBanHistoryRepository : IUserBanHistoryRepository
    {
        private readonly ApplicationDbContext _context;

        public UserBanHistoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserBanHistory?> GetByIdAsync(int id)
        {
            return await _context.UserBanHistories
                .Include(b => b.User)
                .Include(b => b.Admin)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<PagedResult<UserBanHistory>> GetByUserIdAsync(
            string userId,
            UserBanHistoryFilter? filter,
            PagedRequest request)
        {
            var query = _context.UserBanHistories
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(b => b.User)
                .Include(b => b.Admin)
                .Where(b => b.UserId == userId)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = ApplySorting(query, request);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<PagedResult<UserBanHistory>> GetAllAsync(
            UserBanHistoryFilter? filter,
            PagedRequest request)
        {
            var query = _context.UserBanHistories
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(b => b.User)
                .Include(b => b.Admin)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = ApplySorting(query, request);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<UserBanHistory?> GetLatestByUserIdAsync(string userId)
        {
            //Retrieves the most recent record to check current ban status or expiry
            return await _context.UserBanHistories
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BannedAt)
                .FirstOrDefaultAsync();
        }

        public async Task AddAsync(UserBanHistory banHistory)
        {
            await _context.UserBanHistories.AddAsync(banHistory);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        //Helpers

        private static IQueryable<UserBanHistory> ApplyFilter(IQueryable<UserBanHistory> query, UserBanHistoryFilter? filter)
        {
            if (filter == null) return query;

            if (!string.IsNullOrWhiteSpace(filter.UserId))
                query = query.Where(b => b.UserId == filter.UserId);

            if (!string.IsNullOrWhiteSpace(filter.AdminId))
                query = query.Where(b => b.AdminId == filter.AdminId);

            if (filter.IsBanned.HasValue)
                query = query.Where(b => b.IsBanned == filter.IsBanned.Value);

            //Permanent bans has null expiration date
            if (filter.IsPermanent.HasValue)
                query = filter.IsPermanent.Value
                    ? query.Where(b => b.BanExpiresAt == null)
                    : query.Where(b => b.BanExpiresAt != null);

            if (filter.BannedAfter.HasValue)
                query = query.Where(b => b.BannedAt >= filter.BannedAfter.Value);

            if (filter.BannedBefore.HasValue)
                query = query.Where(b => b.BannedAt <= filter.BannedBefore.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.ToLower();
                query = query.Where(b =>
                    b.Reason.ToLower().Contains(search) ||
                    (b.Note != null && b.Note.ToLower().Contains(search)));
            }

            return query;
        }

        private static IQueryable<UserBanHistory> ApplySorting(IQueryable<UserBanHistory> query, PagedRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.SortBy))
                return query.ApplySorting(request.SortBy, request.SortDescending);

            return query.OrderByDescending(b => b.BannedAt);
        }
    }
}