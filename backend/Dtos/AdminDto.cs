using backend.Models;
using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{

    //Returned when admin opens the dashboard — all queue counts in one call
    public class AdminDashboardDto
    {
        //Action queues (things needing attention now)
        public int PendingItemApprovals { get; set; }
        public int PendingLoanApprovals { get; set; }
        public int OpenDisputes { get; set; }
        public int OverdueDisputeResponses { get; set; }
        public int PendingAppeals { get; set; }
        public int PendingUserVerifications { get; set; }
        public int PendingPaymentVerifications { get; set; }
        public int PendingReports { get; set; }

        //User stats
        public int TotalUsers { get; set; }
        public int VerifiedUsers { get; set; }
        public int BannedUsers { get; set; }
        public int NewUsersThisWeek { get; set; }

        //Item stats
        public int TotalActiveItems { get; set; }
        public int ItemsListedThisWeek { get; set; }

        // Loan stats
        public int TotalActiveLoans { get; set; }
        public int OverdueLoans { get; set; }
        public int LoansCreatedThisWeek { get; set; }

        //Financial stats
        public int TotalUnpaidFines { get; set; }
        public decimal TotalUnpaidFinesAmount { get; set; }
        public decimal FinesCollectedThisMonth { get; set; }
        public int FinesIssuedThisMonth { get; set; }

        //Dispute health
        public int DisputesResolvedThisMonth { get; set; }
        public double AverageDisputeResolutionDays { get; set; }
    }
    //Admin looks up an item's full audit trail by item ID
    public class ItemHistoryDto
        {
            public int ItemId { get; set; }
            public string ItemTitle { get; set; } = string.Empty;
            public string OwnerName { get; set; } = string.Empty;
            public double AverageRating { get; set; }
            public int ReviewCount { get; set; }
            public List<ItemReviewEntryDto> Reviews { get; set; } = new();
            public List<LoanHistoryEntryDto> Loans { get; set; } = new();
        }

    public class ItemReviewEntryDto
    {
        public int Id { get; set; }
        public string ReviewerId { get; set; } = string.Empty;
        public string? ReviewerAvatarUrl { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public bool IsAdminReview { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LoanHistoryEntryDto
    {
        public int LoanId { get; set; }
        public string BorrowerName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? ActualReturnDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string SnapshotCondition { get; set; } = string.Empty;
        //public List<LoanSnapshotPhotoDto> SnapshotPhotos { get; set; } = new();
        //public List<FineResponseDto> Fines { get; set; } = new();
        //public List<DisputeDto.DisputeSummaryDto> Disputes { get; set; } = new();
        
}

 
    public class AdminRejectItemDto { public string Reason { get; set; } = string.Empty; }
    public class AdminForceCancelLoanDto { public string Reason { get; set; } = string.Empty; }
    public class AdminRejectPaymentDto { public string Reason { get; set; } = string.Empty; }
    public class AdminRejectDto { public string Reason { get; set; } = string.Empty; }
    public class AdminSendNotificationDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
    }
    public class AdminBroadcastNotificationDto
    {
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
    }


}