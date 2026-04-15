using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{
    public class SendSupportMessageDto
    {
        [Required(ErrorMessage = "Message content is required")]
        [MinLength(1, ErrorMessage = "Message cannot be empty")]
        [MaxLength(2000, ErrorMessage = "Message cannot exceed 2000 characters")]
        public string Content { get; set; } = string.Empty;
    }

    public class SupportMessageDto
    {
        public int Id { get; set; }
        public int SupportThreadId { get; set; }

        //Sender info
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderFullName { get; set; } = string.Empty;
        public string? SenderAvatarUrl { get; set; }
        public bool IsAdminMessage { get; set; } //True if sender is admin
        public bool IsMine { get; set; } //Current user sent this

        public string Content { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public DateTime SentAt { get; set; }
    }

    public class SupportMessageListDto
    {
        public int Id { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderUserName { get; set; } = string.Empty;
        public string? SenderAvatarUrl { get; set; }
        public bool IsAdmin { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsMine { get; set; }
    }

    public class MarkSupportMessagesReadDto
    {
        public int? UpToMessageId { get; set; } //Optional: mark up to specific message
    }

    //Unread count response
    public class SupportUnreadCountDto
    {
        public int SupportThreadId { get; set; }
        public int UnreadCount { get; set; }
    }





}
