using backend.Models;
using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{
  
    public class CreateVerificationRequestDto
    {
        [Required]
        public VerificationDocumentType DocumentType { get; set; }

        [Required]
        public string DocumentUrl { get; set; } = string.Empty;
    }

    public class AdminDecideVerificationRequestDto
    {
        [Required]
        public VerificationStatus Status { get; set; }

        [MaxLength(1000)]
        public string? AdminNote { get; set; }
    }

    public class VerificationRequestDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? UserAvatarUrl { get; set; }
        public VerificationDocumentType DocumentType { get; set; }
        public string DocumentUrl { get; set; } = string.Empty;
        public VerificationStatus Status { get; set; }
        public string? AdminNote { get; set; }
        public string? ReviewedByAdminId { get; set; }
        public string? ReviewedByAdminName { get; set; }
        public string? ReviewedByAdminAvatarUrl { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }

    //Lightweight list — for admin review queue
    public class VerificationRequestListDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? UserAvatarUrl { get; set; }
        public VerificationDocumentType DocumentType { get; set; }
        public VerificationStatus Status { get; set; }
        public string? ReviewedByAdminName { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }





    
}