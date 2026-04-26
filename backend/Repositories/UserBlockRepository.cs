using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class UserBlockRepository : IUserBlockRepository
    {
        private readonly ApplicationDbContext _context;

        public UserBlockRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserBlock?> GetAsync(string blockerId, string blockedId)
        {
            return await _context.UserBlocks
                .Include(b => b.Blocker)
                .Include(b => b.Blocked)
                .FirstOrDefaultAsync(b => b.BlockerId == blockerId && b.BlockedId == blockedId);
        }

        public async Task<PagedResult<UserBlock>> GetBlocksByBlockerAsync(
            string blockerId,
            UserBlockFilter? filter,
            PagedRequest request)
        {
            var query = _context.UserBlocks
                .AsNoTracking()
                .Include(b => b.Blocker)
                .Include(b => b.Blocked)
                .Where(b => b.BlockerId == blockerId)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = ApplySorting(query, request);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<PagedResult<UserBlock>> GetAllAsync(
            UserBlockFilter? filter,
            PagedRequest request)
        {
            var query = _context.UserBlocks
                .AsNoTracking()
                .Include(b => b.Blocker)
                .Include(b => b.Blocked)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = ApplySorting(query, request);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<bool> IsBlockedAsync(string blockerId, string blockedId)
        {
            return await _context.UserBlocks
                .AnyAsync(b => b.BlockerId == blockerId && b.BlockedId == blockedId);
        }

        public async Task<bool> AreBlockedEitherWayAsync(string userId1, string userId2)
        {
            //Checks for a block relationship regardless of who initiated it
            return await _context.UserBlocks
                .AnyAsync(b =>
                    (b.BlockerId == userId1 && b.BlockedId == userId2) ||
                    (b.BlockerId == userId2 && b.BlockedId == userId1));
        }

        //Returns all user IDs that are in a block relationship with userId (either direction)
        public async Task<HashSet<string>> GetBlockedUserIdsAsync(string userId)
        {
            var ids = await _context.UserBlocks
                .Where(b => b.BlockerId == userId || b.BlockedId == userId)
                .Select(b => b.BlockerId == userId ? b.BlockedId : b.BlockerId)
                .ToListAsync();

            //HashSet provides O(1) lookup time for subsequent permission checks
            return ids.ToHashSet();
        }

        //Outgoing only — used for DM display so the blocked person doesn't know they're blocked
        public async Task<HashSet<string>> GetOutgoingBlockedUserIdsAsync(string userId)
        {
            var ids = await _context.UserBlocks
                .Where(b => b.BlockerId == userId) //only blocks THIS user initiated
                .Select(b => b.BlockedId)
                .ToListAsync();

            return ids.ToHashSet();
        }

        public async Task AddAsync(UserBlock block)
        {
            await _context.UserBlocks.AddAsync(block);
        }

        public Task DeleteAsync(UserBlock block)
        {
            _context.UserBlocks.Remove(block);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        //Helpers

        private static IQueryable<UserBlock> ApplyFilter(IQueryable<UserBlock> query, UserBlockFilter? filter)
        {
            if (filter == null) return query;

            if (!string.IsNullOrWhiteSpace(filter.BlockerId))
                query = query.Where(b => b.BlockerId == filter.BlockerId);

            if (!string.IsNullOrWhiteSpace(filter.BlockedId))
                query = query.Where(b => b.BlockedId == filter.BlockedId);

            if (filter.CreatedAfter.HasValue)
                query = query.Where(b => b.CreatedAt >= filter.CreatedAfter.Value);

            if (filter.CreatedBefore.HasValue)
                query = query.Where(b => b.CreatedAt <= filter.CreatedBefore.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.ToLower();
                query = query.Where(b =>
                    b.Blocked.FullName.ToLower().Contains(search) ||
                    b.Blocked.UserName!.ToLower().Contains(search) ||
                    b.Blocker.FullName.ToLower().Contains(search) ||
                    b.Blocker.UserName!.ToLower().Contains(search));
            }

            return query;
        }

        private static IQueryable<UserBlock> ApplySorting(IQueryable<UserBlock> query, PagedRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.SortBy))
                return query.ApplySorting(request.SortBy, request.SortDescending);

            return query.OrderByDescending(b => b.CreatedAt);
        }
    }
}