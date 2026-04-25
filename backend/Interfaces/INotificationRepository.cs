using backend.Dtos;
using backend.Models;

namespace backend.Interfaces
{
    public interface INotificationRepository
    {
        Task<PagedResult<NotificationDto>> GetPagedAsync(string userId, NotificationFilter filter, PagedRequest request);
        Task<List<Notification>> GetByUserIdAsync(string userId);
        Task<Notification?> GetByIdAsync(int id);
        Task AddAsync(Notification notification);
        void Delete(Notification notification);
        Task DeleteAllByUserIdAsync(string userId);
        Task MarkAllAsReadByUserIdAsync(string userId);
        Task MarkMultipleAsReadAsync(List<int> notificationIds, string userId);
        Task SaveChangesAsync();
    }
}