using backend.Models;
using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{

    public class CreateLoanDisputeFineDto
    {
        [Required]
        public string UserId { get; set; } = string.Empty;
        [Required]
        public decimal Amount { get; set; }
        public int LoanId { get; set; }
        public int DisputeId { get; set; }
        [MaxLength(1000)]
        public string? AdminNote { get; set; }
    }

    public class CreateCustomFineDto
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public decimal Amount { get; set; }

        [Required, MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;
    }

    public class SubmitPaymentProofDto
    {
        [Required]
        public int FineId { get; set; }

        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        [Required, MaxLength(1000)]
        public string PaymentDescription { get; set; } = string.Empty;

        [Required]
        public string PaymentProofImageUrl { get; set; } = string.Empty;
    }

   
    //Admin updates a fine
    public class UpdateFineDto
    {
        [Required]
        public int FineId { get; set; }
        public decimal? Amount { get; set; }
        public string? Reason { get; set; }
        public FineStatus? Status { get; set; }
    }

    //Admin confirms or rejects a fine payment
    public class AdminFineVerifyPaymentDto
    {

        [Required]
        public bool IsApproved { get; set; }

        public string? RejectionReason { get; set; } //Required if rejected
    }

    public class FineDto
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? UserAvatarUrl { get; set; }
        public bool IsMine { get; set; }

        public int? DisputeId { get; set; }
        public int? LoanId { get; set; }
        public string? ItemTitle { get; set; }
        public string? ItemSlug { get; set; }

        public FineType Type { get; set; }
        public FineStatus Status { get; set; }
        public decimal Amount { get; set; }

        public decimal? ItemValueAtTimeOfFine { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public string? PaymentProofImageUrl { get; set; }
        public string? PaymentDescription { get; set; }
        public string? RejectionReason { get; set; }

        public string? IssuedByAdminId { get; set; }
        public string? IssuedByAdminName { get; set; }
        public string? IssuedByAdminUserName { get; set; }
        public string? IssuedByAdminAvatarUrl { get; set; }
        public string? AdminNote { get; set; }

        public string? VerifiedByAdminId { get; set; }
        public string? VerifiedByAdminName { get; set; }
        public string? VerifiedByAdminUserName { get; set; }
        public string? VerifiedByAdminAvatarUrl { get; set; }


        public bool HasPendingAppeal { get; set; } 
        public int? ActiveAppealId { get; set; }

        public DateTime? ProofSubmittedAt { get; set; }
        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class FineListDto
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? UserAvatarUrl { get; set; }

        public int? LoanId { get; set; }
        public int? DisputeId { get; set; }
        public string? ItemTitle { get; set; }

        public FineType Type { get; set; }
        public FineStatus Status { get; set; }
        public decimal Amount { get; set; }

        public bool HasPendingAppeal { get; set; }


        public string IssuedByAdminId { get; set; } = string.Empty;
        public string IssuedByAdminName { get; set; } = string.Empty;
        public string IssuedByAdminUsername { get; set; } = string.Empty;
        public string? IssuedByAdminUserAvatarUrl { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
    }

    public class FineStatsDto
    {
        public int TotalUnpaid { get; set; }
        public int PendingProofReview { get; set; }
        public decimal TotalOutstandingAmount { get; set; }
        public int IssuedThisMonth { get; set; }
        public Dictionary<FineStatus, int> StatusBreakdown { get; set; } = new();
        public Dictionary<FineType, int> TypeBreakdown { get; set; } = new();
    }


}
