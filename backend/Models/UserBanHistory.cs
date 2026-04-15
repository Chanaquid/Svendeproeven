using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class UserBanHistory
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        [Required]
        public string AdminId { get; set; } = string.Empty;

        [ForeignKey(nameof(AdminId))]
        public ApplicationUser Admin { get; set; } = null!;


        [Required]
        public bool IsBanned { get; set; } //true = ban, false = unban

        [Required, MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        public string? Note {  get; set; }

        [Required]
        public DateTime BannedAt { get; set; } = DateTime.UtcNow;

        public DateTime? BanExpiresAt { get; set; } // null = permanent ban
    }
}
