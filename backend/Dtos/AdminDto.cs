using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{

        //Returned when admin opens the dashboard — all queue counts in one call
        public class AdminDashboardDto
        {
            //Action queues
            public int PendingItemApprovals { get; set; }
            public int PendingLoanApprovals { get; set; }
            public int OpenDisputes { get; set; }
            public int PendingAppeals { get; set; }
            public int PendingUserVerifications { get; set; }
            public int PendingPaymentVerifications { get; set; }
            public int PendingReports { get; set; }

            //Platform stats
            public int TotalUsers { get; set; }
            public int TotalActiveItems { get; set; }
            public int TotalActiveLoans { get; set; }
            public int TotalUnpaidFines { get; set; }
            public decimal TotalUnpaidFinesAmount { get; set; }
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

        public class AdminBanRequestDto
        {
            [Required, MaxLength(500)]
            public string Reason { get; set; } = string.Empty;

            //Optional duration in minutes; null = permanent ban
            public int? DurationMinutes { get; set; }
        }

        public class AdminUnBanRequestDto
        {
            //Optional note explaining why the ban was lifted
            [MaxLength(500)]
            public string? Note { get; set; } = string.Empty;
        }

        public class BanHistoryResponseDto
        {
            public int Id { get; set; }

            //User info
            public string UserId { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string? UserAvatarUrl { get; set; }

            //Admin info
            public string AdminId { get; set; } = string.Empty;
            public string AdminName { get; set; } = string.Empty;
            public string? AdminAvatarUrl { get; set; }

            //Ban info
            public bool IsBanned { get; set; } //true = ban, false = unban
            public string Reason { get; set; } = string.Empty;
            public string? Note { get; set; }

            public DateTime BannedAt { get; set; }
            public DateTime? BanExpiresAt { get; set; }
        }


    
}