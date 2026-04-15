using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{
    public class SendLoanMessageDto
    {
        [Required(ErrorMessage = "Message content is required")]
        [MinLength(1, ErrorMessage = "Message cannot be empty")]
        [MaxLength(2000, ErrorMessage = "Message cannot exceed 2000 characters")]
        public string Content { get; set; } = string.Empty;
    }

    public class LoanMessageDto
    {
        public int Id { get; set; }
        public int LoanId { get; set; }

        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string? SenderAvatarUrl { get; set; }
        public bool IsMine { get; set; }

        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime SentAt { get; set; }
    }

    public class MarkLoanMessagesReadDto
    {
        public int? UpToMessageId { get; set; } //Optional? mark up to specific message
    }

    public class LoanUnreadCountDto
    {
        public int LoanId { get; set; }
        public int UnreadCount { get; set; }

    }
}
