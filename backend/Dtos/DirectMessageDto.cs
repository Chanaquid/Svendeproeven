using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{
    public class SendDirectMessageDto
    {
 
        [Required]
        [MinLength(1), MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

    }

    public class DirectMessageDto
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }

        public string SenderId { get; set; } = string.Empty;
        public string SenderFullName { get; set; } = string.Empty;
        public string SenderUserName { get; set; } = string.Empty;
        public string? SenderAvatarUrl { get; set; }

        public string Content { get; set; } = string.Empty;

        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }

        public DateTime SentAt { get; set; }

        public bool IsMine { get; set; }
        //public string TimeAgo => GetTimeAgo(SentAt);
        //public string ReadTimeAgo => ReadAt.HasValue ? GetTimeAgo(ReadAt.Value) : "Not read yet";

        //private static string GetTimeAgo(DateTime dateTime)
        //{
        //    var timeSpan = DateTime.UtcNow - dateTime;

        //    if (timeSpan.TotalMinutes < 1)
        //        return "Just now";
        //    if (timeSpan.TotalMinutes < 60)
        //        return $"{timeSpan.Minutes} min ago";
        //    if (timeSpan.TotalHours < 24)
        //        return $"{timeSpan.Hours} hour{(timeSpan.Hours > 1 ? "s" : "")} ago";
        //    if (timeSpan.TotalDays < 7)
        //        return $"{timeSpan.Days} day{(timeSpan.Days > 1 ? "s" : "")} ago";

        //    return dateTime.ToString("MMM dd, yyyy");
        //}

    }


}
