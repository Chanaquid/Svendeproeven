using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class SupportRepository : ISupportRepository
    {
        private readonly ApplicationDbContext _context;

        public SupportRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        //SupportThread
        public async Task<SupportThread?> GetThreadByIdAsync(int id)
        {
            return await _context.SupportThreads
                .Include(t => t.User)
                .Include(t => t.ClaimedByAdmin)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<SupportThread?> GetThreadByIdWithMessagesAsync(int id)
        {
            return await _context.SupportThreads
                .Include(t => t.User)
                .Include(t => t.ClaimedByAdmin)
                .Include(t => t.Messages.OrderBy(m => m.SentAt))
                    .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<SupportThread?> GetActiveThreadByUserIdAsync(string userId)
        {
            return await _context.SupportThreads
                .Include(t => t.User)
                .Include(t => t.ClaimedByAdmin)
                .FirstOrDefaultAsync(t =>
                    t.UserId == userId &&
                    t.Status != SupportThreadStatus.Closed);
        }

        public async Task<PagedResult<SupportThread>> GetThreadsByUserIdAsync(
            string userId,
            SupportThreadFilter? filter,
            PagedRequest request)
        {
            var query = _context.SupportThreads
                .AsNoTracking()
                .Include(t => t.User)
                .Include(t => t.ClaimedByAdmin)
                .Where(t => t.UserId == userId)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = ApplySorting(query, request);

            return await query.ToPagedResultAsync(request);
        }

        public async Task<PagedResult<SupportThread>> GetAllThreadsAsync(
            SupportThreadFilter? filter,
            PagedRequest request)
        {
            var query = _context.SupportThreads
                .AsNoTracking()
                .Include(t => t.User)
                .Include(t => t.ClaimedByAdmin)
                .AsQueryable();

            query = ApplyFilter(query, filter);
            query = ApplySorting(query, request);

            return await query.ToPagedResultAsync(request);
        }

        //Used by background job to auto-close inactive threads
        public async Task<List<SupportThread>> GetInactiveOpenThreadsAsync(DateTime inactiveSince)
        {
            //A thread is inactive if no message has been sent since inactiveSince
            return await _context.SupportThreads
                .Where(t =>
                    t.Status != SupportThreadStatus.Closed &&
                    !t.Messages.Any(m => m.SentAt >= inactiveSince))
                .ToListAsync();
        }

        public async Task<bool> HasActiveThreadAsync(string userId)
        {
            return await _context.SupportThreads
                .AnyAsync(t =>
                    t.UserId == userId &&
                    t.Status != SupportThreadStatus.Closed);
        }

        public async Task AddThreadAsync(SupportThread thread)
        {
            await _context.SupportThreads.AddAsync(thread);
        }

        public void UpdateThread(SupportThread thread)
        {
            _context.SupportThreads.Update(thread);
        }

        //Support message

        public async Task<SupportMessage?> GetMessageByIdAsync(int id)
        {
            return await _context.SupportMessages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<PagedResult<SupportMessage>> GetMessagesAsync(int threadId, PagedRequest request)
        {
            var query = _context.SupportMessages
                .AsNoTracking()
                .Include(m => m.Sender)
                .Where(m => m.SupportThreadId == threadId)
                .AsQueryable();

            var sortBy = string.IsNullOrWhiteSpace(request.SortBy) ? "SentAt" : request.SortBy;
            query = query.ApplySorting(sortBy, request.SortDescending);

            return await query.ToPagedResultAsync(request);
        }

        public async Task AddMessageAsync(SupportMessage message)
        {
            await _context.SupportMessages.AddAsync(message);
        }

        public async Task MarkMessagesAsReadAsync(int threadId, string userId, int? upToMessageId = null)
        {
            var query = _context.SupportMessages
                .Where(m => m.SupportThreadId == threadId &&
                            m.SenderId != userId &&
                            !m.IsRead);

            if (upToMessageId.HasValue)
                query = query.Where(m => m.Id <= upToMessageId.Value);

            await query.ExecuteUpdateAsync(s => s
                .SetProperty(m => m.IsRead, true));
        }

        public async Task<int> GetUnreadCountAsync(int threadId, string userId)
        {
            return await _context.SupportMessages
                .CountAsync(m =>
                    m.SupportThreadId == threadId &&
                    m.SenderId != userId &&
                    !m.IsRead);
        }

        public async Task<SupportMessage?> GetLastMessageAsync(int threadId)
        {
            return await _context.SupportMessages
                .AsNoTracking()
                .Include(m => m.Sender)
                .Where(m => m.SupportThreadId == threadId)
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefaultAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<int> CountAsync(SupportThreadFilter filter)
        {
            var query = _context.SupportThreads.AsQueryable();
            if (filter.Status.HasValue)
                query = query.Where(x => x.Status == filter.Status.Value);
            return await query.CountAsync();

        }

        //Helpers

        private static IQueryable<SupportThread> ApplyFilter(
            IQueryable<SupportThread> query,
            SupportThreadFilter? filter)
        {
            if (filter == null) return query;

            if (!string.IsNullOrWhiteSpace(filter.UserId))
                query = query.Where(t => t.UserId == filter.UserId);

            if (!string.IsNullOrWhiteSpace(filter.ClaimedByAdminId))
                query = query.Where(t => t.ClaimedByAdminId == filter.ClaimedByAdminId);

            if (filter.Status.HasValue)
                query = query.Where(t => t.Status == filter.Status.Value);

           
            if (!filter.Status.HasValue)
            {
                if (filter.IsClaimed == true)
                    query = query.Where(t => t.ClaimedByAdminId != null);

                if (filter.IsUnclaimed == true)
                    query = query.Where(t => t.ClaimedByAdminId == null);
            }

            if (filter.CreatedAfter.HasValue)
                query = query.Where(t => t.CreatedAt >= filter.CreatedAfter.Value);

            if (filter.CreatedBefore.HasValue)
                query = query.Where(t => t.CreatedAt <= filter.CreatedBefore.Value);

            if (filter.ClaimedAfter.HasValue)
                query = query.Where(t => t.ClaimedAt.HasValue && t.ClaimedAt.Value >= filter.ClaimedAfter.Value);

            if (filter.ClaimedBefore.HasValue)
                query = query.Where(t => t.ClaimedAt.HasValue && t.ClaimedAt.Value <= filter.ClaimedBefore.Value);

            if (filter.ClosedAfter.HasValue)
                query = query.Where(t => t.ClosedAt.HasValue && t.ClosedAt.Value >= filter.ClosedAfter.Value);

            if (filter.ClosedBefore.HasValue)
                query = query.Where(t => t.ClosedAt.HasValue && t.ClosedAt.Value <= filter.ClosedBefore.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.ToLower();
                query = query.Where(t =>
                    t.Subject.ToLower().Contains(search) ||
                    t.User.FullName.ToLower().Contains(search) ||
                    t.User.UserName!.ToLower().Contains(search));
            }

            return query;
        }

        private static IQueryable<SupportThread> ApplySorting(
            IQueryable<SupportThread> query,
            PagedRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.SortBy))
                return query.ApplySorting(request.SortBy, request.SortDescending);

            return query.OrderByDescending(t => t.CreatedAt);
        }
    }
}