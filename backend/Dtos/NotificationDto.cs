using backend.Models;
using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{
    //list?
    public class NotificationSummaryDto
    {
        public int UnreadCount { get; set; }
        public List<NotificationDto> Recent { get; set; } = new();
    }
    public class NotificationDto
    {
        public int Id { get; set; }

        public NotificationType Type { get; set; }
        public string Message { get; set; } = string.Empty;

        public int? ReferenceId { get; set; }
        public NotificationReferenceType? ReferenceType { get; set; }
        
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

    }

    public class AdminNotificationDto
    {
        public int Id { get; set; }

        //Receiver info (for admin view)
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? UserAvatarUrl { get; set; }

        //Notification details
        public NotificationType Type { get; set; }
        public string Message { get; set; } = string.Empty;

        //Related entity
        public int? ReferenceId { get; set; }
        public NotificationReferenceType? ReferenceType { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class MarkMultipleNotificationsReadDto
    {
        [Required(ErrorMessage = "Notification IDs are required")]
        public List<int> NotificationIds { get; set; } = new();
    }

    public class UnreadNotificationCountDto
    {
        public int UnreadCount { get; set; }
    }

  

   

}