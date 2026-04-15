using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{
    //Admin bans a user

    public class BanUserDto
    {
        [Required, MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        public DateTime? BanExpiresAt { get; set; } //null = permanent
    }

    //Admin unbans a user
    public class UnbanUserDto
    {
        [Required, MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Note { get; set; }
    }

    
    public class UserBanHistoryDto
    {
        public int Id { get; set; }
        public string BannedUserId { get; set; } = string.Empty;
        public string BannedFullName { get; set; } = string.Empty;
        public string BannedUserName { get; set; } = string.Empty;
        public string? BannedUserAvatarUrl { get; set; }
        public string AdminId { get; set; } = string.Empty;
        public string AdminFullName { get; set; } = string.Empty;
        public string AdminUserName { get; set; } = string.Empty;
        public string? AdminAvatarUrl { get; set; }

        public bool IsBanned { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Note { get; set; }

        public DateTime BannedAt { get; set; }
        public DateTime? BanExpiresAt { get; set; }
        public bool IsPermanent => BanExpiresAt == null;

        public bool IsActiveBan => IsBanned && (BanExpiresAt == null || BanExpiresAt > DateTime.UtcNow);
    }

    public class UserBanHistoryListDto
    {
        public int Id { get; set; }
        public string AdminName { get; set; } = string.Empty;
        public string AdminUserName { get; set; } = string.Empty;
        public bool IsBanned { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime BannedAt { get; set; }
        public DateTime? BanExpiresAt { get; set; }
        public bool IsPermanent => BanExpiresAt == null;
    }


}
