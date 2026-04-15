using backend.Models;
using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{

    //User files a report
    public class CreateReportDto
    {
        [Required]
        public ReportType Type { get; set; }

        [Required]
        public string TargetId { get; set; } = string.Empty;

        [Required]
        public ReportReason Reasons { get; set; }

        [MaxLength(2000)]
        public string? AdditionalDetails { get; set; }
    }

    public class AdminResolveReportDto
    {

        [Required(ErrorMessage = "Resolution status is required")]
        public ReportStatus Status { get; set; }

        [MaxLength(1000, ErrorMessage = "Admin note cannot exceed 1000 characters")]
        public string? AdminNote { get; set; }
    }

    public class AdminUpdateReportStatusDto
    {

        [Required]
        public ReportStatus Status { get; set; }

        [MaxLength(1000, ErrorMessage = "Admin note cannot exceed 1000 characters")]
        public string? AdminNote { get; set; }
    }

    public class ReportDto
    {
        public int Id { get; set; }

        public string ReportedById { get; set; } = string.Empty;
        public string ReportedByName { get; set; } = string.Empty;
        public string ReportedByUserName { get; set; } = string.Empty;
        public string? ReportedByAvatarUrl { get; set; }
        public bool IsMine { get; set; }

        //Report details
        public ReportType Type { get; set; }
        public string TargetId { get; set; } = string.Empty;
        public ReportReason Reasons { get; set; }
        public string? AdditionalDetails { get; set; }
        public ReportStatus Status { get; set; }

        public string? HandledByAdminId { get; set; }
        public string? HandledByAdminName { get; set; }
        public string? HandledByAdminUserName { get; set; }
        public string? HandledByAdminAvatarUrl { get; set; }
        public string? AdminNote { get; set; }
        public DateTime? ResolvedAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }


    //Lightweight list view for admin queue
    public class ReportListDto
    {
        public int Id { get; set; }
        public string ReportedByName { get; set; } = string.Empty;
        public string ReportedByUserName { get; set; } = string.Empty;
        public ReportType Type { get; set; }
        public string TargetId { get; set; } = string.Empty;
        public ReportReason Reasons { get; set; }
        public ReportStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    } 
    
}