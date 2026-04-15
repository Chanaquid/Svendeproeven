using backend.Dtos;
using backend.Models;

namespace backend.Interfaces
{
    public interface INotificationService
    {
        //User actions
        Task<NotificationSummaryDto> GetSummaryAsync(string userId);
        Task<List<NotificationDto>> GetAllAsync(string userId);
        Task MarkAsReadAsync(int notificationId, string userId);
        Task MarkAllAsReadAsync(string userId);
        Task DeleteAsync(int notificationId, string userId);
        Task DeleteAllAsync(string userId);

        //Internal — called by other services, not controllers
        Task SendAsync(string userId, NotificationType type, string message, int? referenceId = null, NotificationReferenceType? referenceType = null);
        Task SendToMultipleAsync(List<string> userIds, NotificationType type, string message, int? referenceId = null, NotificationReferenceType? referenceType = null);

        Task MarkMultipleAsReadAsync(List<int> notificationIds, string userId);

        Task SendToAdminsAsync(NotificationType type, string message, int? referenceId = null,
            NotificationReferenceType? referenceType = null);


    }
}