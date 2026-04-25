using backend.Data;
using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _context;

        public NotificationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<NotificationDto>> GetPagedAsync(string userId, NotificationFilter filter, PagedRequest request)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId)
                .AsQueryable();

            //Filters
            if (filter.IsRead.HasValue)
                query = query.Where(n => n.IsRead == filter.IsRead.Value);

            if (filter.Type.HasValue)
                query = query.Where(n => n.Type == filter.Type.Value);

            if (filter.ReferenceType.HasValue)
                query = query.Where(n => n.ReferenceType == filter.ReferenceType.Value);

            if (filter.CreatedAfter.HasValue)
                query = query.Where(n => n.CreatedAt >= filter.CreatedAfter.Value);

            if (filter.CreatedBefore.HasValue)
                query = query.Where(n => n.CreatedAt <= filter.CreatedBefore.Value);

            // Sorting
            query = (request.SortBy?.ToLower(), request.SortDescending) switch
            {
                ("type", false) => query.OrderBy(n => n.Type),
                ("type", true) => query.OrderByDescending(n => n.Type),
                ("isread", false) => query.OrderBy(n => n.IsRead).ThenByDescending(n => n.CreatedAt),
                ("isread", true) => query.OrderByDescending(n => n.IsRead).ThenByDescending(n => n.CreatedAt),
                _ => query.OrderByDescending(n => n.CreatedAt), //default: newest first
            };

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Type = n.Type,
                    Message = n.Message,
                    ReferenceId = n.ReferenceId,
                    ReferenceType = n.ReferenceType,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<NotificationDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<List<Notification>> GetByUserIdAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<Notification?> GetByIdAsync(int id)
        {
            return await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task AddAsync(Notification notification)
        {
            await _context.Notifications.AddAsync(notification);
        }

        public void Delete(Notification notification)
        {
            _context.Notifications.Remove(notification);
        }


        //Bulk delete
        public async Task DeleteAllByUserIdAsync(string userId)
        {
            await _context.Notifications
                .Where(n => n.UserId == userId)
                .ExecuteDeleteAsync();
        }


        //Mark specific notifications as read
        public async Task MarkMultipleAsReadAsync(List<int> notificationIds, string userId)
        {
            await _context.Notifications
                .Where(n => notificationIds.Contains(n.Id) && n.UserId == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        }

        //Mark all as read for a user — bulk update
        public async Task MarkAllAsReadByUserIdAsync(string userId)
        {
            await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}