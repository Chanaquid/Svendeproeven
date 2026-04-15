using backend.Data;
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