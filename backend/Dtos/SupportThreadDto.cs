using backend.Models;
using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{
    public class CreateSupportThreadDto
    {
        [Required, MaxLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string InitialMessage { get; set; } = string.Empty;
    }

    // Admin claims or updates thread status
    public class UpdateSupportThreadStatusDto
    {
        [Required]
        public SupportThreadStatus Status { get; set; }
    }

    //Full thread detail — includes messages
    public class SupportThreadDto
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? UserAvatarUrl { get; set; }

        public string? ClaimedByAdminId { get; set; }
        public string? ClaimedByAdminName { get; set; }
        public string? ClaimedByAdminUserName { get; set; }
        public string? ClaimedByAdminAvatarUrl { get; set; }

        public SupportThreadStatus Status { get; set; }
        public DateTime? ClaimedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<SupportMessageDto> Messages { get; set; } = new();
    }

    //Lightweight list — for inbox/queue views
    public class SupportThreadListDto
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? UserAvatarUrl { get; set; }
        public string? ClaimedByAdminName { get; set; }
        public SupportThreadStatus Status { get; set; }
        public string? LastMessagePreview { get; set; }  // First ~100 chars of last message
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }


    public class ClaimSupportThreadDto
    {
        [Required]
        public int ThreadId { get; set; }
    }

}
