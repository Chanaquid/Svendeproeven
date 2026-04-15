using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class Report
    {
        public int Id { get; set; }

        //Who filed the report
        public string ReportedById { get; set; } = string.Empty;
        public ApplicationUser ReportedBy { get; set; } = null!;

        //Who/what is being reported
        public ReportType Type { get; set; }

        //The ID of the reported entity (UserId, ItemId, ReviewId, MessageId)
        public string TargetId { get; set; } = string.Empty;

        //Reason - only 1 allowed
        public ReportReason Reasons { get; set; }

        public string? AdditionalDetails { get; set; } //free text from reporter

        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        //Admin handling
        public string? HandledByAdminId { get; set; }
        public ApplicationUser? HandledByAdmin { get; set; }

        public string? AdminNote { get; set; }
        public DateTime? ResolvedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

 
}