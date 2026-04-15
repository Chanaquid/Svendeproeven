using backend.Dtos;
using backend.Hubs;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace backend.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationService(INotificationRepository notificationRepository,
            IHubContext<NotificationHub> hubContext,
            UserManager<ApplicationUser> userManager)
        {
            _notificationRepository = notificationRepository;
            _hubContext = hubContext;
            _userManager = userManager;
        }

        //Get summary
        public async Task<NotificationSummaryDto> GetSummaryAsync(string userId)
        {
            var all = await _notificationRepository.GetByUserIdAsync(userId);

            return new NotificationSummaryDto
            {
                UnreadCount = all.Count(n => !n.IsRead),
                Recent = all.Take(10).Select(MapToNotificationDTO).ToList()
            };
        }

        //Get all notifications for a user
        public async Task<List<NotificationDto>> GetAllAsync(string userId)
        {
            var notifications = await _notificationRepository.GetByUserIdAsync(userId);
            return notifications.Select(MapToNotificationDTO).ToList();
        }

        //Mark a notification as read
        public async Task MarkAsReadAsync(int notificationId, string userId)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);

            if (notification == null || notification.UserId != userId)
                throw new KeyNotFoundException("Notification not found.");

            if (notification.IsRead) return;

            notification.IsRead = true;
            await _notificationRepository.SaveChangesAsync();
        }

        //Mark notifications as read
        public async Task MarkMultipleAsReadAsync(List<int> notificationIds, string userId)
        {
            if (!notificationIds.Any()) return;
            await _notificationRepository.MarkMultipleAsReadAsync(notificationIds, userId);
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            await _notificationRepository.MarkAllAsReadByUserIdAsync(userId);
        }

        public async Task SendToMultipleAsync(List<string> userIds, NotificationType type, string message,
            int? referenceId = null, NotificationReferenceType? referenceType = null)
        {
            foreach (var userId in userIds)
                await SendAsync(userId, type, message, referenceId, referenceType);
        }

        public async Task DeleteAsync(int notificationId, string userId)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification == null || notification.UserId != userId)
                throw new KeyNotFoundException("Notification not found.");

            _notificationRepository.Delete(notification);
            await _notificationRepository.SaveChangesAsync();
        }

        public async Task DeleteAllAsync(string userId)
        {
            await _notificationRepository.DeleteAllByUserIdAsync(userId);

        }

        //send notificaiton
        public async Task SendAsync(
            string userId,
            NotificationType type,
            string message,
            int? referenceId = null,
            NotificationReferenceType? referenceType = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Message = message,
                ReferenceId = referenceId,
                ReferenceType = referenceType,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddAsync(notification);
            await _notificationRepository.SaveChangesAsync();

            //await _hubContext.Clients
            //   .Group($"user_{userId}")
            //   .SendAsync("NewNotification", new
            //   {
            //       id = notification.Id,
            //       message = notification.Message,
            //       type = notification.Type.ToString(),
            //       referenceId = notification.ReferenceId,
            //       referenceType = notification.ReferenceType?.ToString(),
            //       isRead = false,
            //       createdAt = notification.CreatedAt
            //   });


        }

        //To admins
        public async Task SendToAdminsAsync(
            NotificationType type,
            string message,
            int? referenceId = null,
            NotificationReferenceType? referenceType = null)
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");

            if (!admins.Any()) return;

            foreach (var admin in admins)
                await SendAsync(admin.Id, type, message, referenceId, referenceType);
        }



        //Map to DTO
        private static NotificationDto MapToNotificationDTO(Notification n)
        {
            return new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Message = n.Message,
                ReferenceId = n.ReferenceId,
                ReferenceType = n.ReferenceType,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            };
        }

    }
}
