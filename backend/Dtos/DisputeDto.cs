using backend.Dtos;
using backend.Models;
using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{

    //Either party files a dispute on a completed or active loan
    public class CreateDisputeDto
    {
        [Required]
        public int LoanId { get; set; }

        [Required]
        public DisputeFiledAs FiledAs { get; set; }

        [Required, MinLength(20), MaxLength(2000)] 
        public string Description { get; set; } = string.Empty;
    }

    //Other party submits their side within the 72h window
    public class SubmitDisputeResponseDto
    {
        [Required, MinLength(20), MaxLength(2000)]
        public string ResponseDescription { get; set; } = string.Empty;
    }

    public class EditDisputeDto
    {
        [Required, MinLength(20), MaxLength(2000)]
        public string Description { get; set; } = string.Empty;
    }

    //Admin issues their final verdict
    public class AdminResolveDisputeDto
    {
        [Required]
        public DisputeVerdict Verdict { get; set; }

        public string? AdminNote { get; set; }

        public DisputePenaltyDto? OwnerPenalty { get; set; }
        public DisputePenaltyDto? BorrowerPenalty { get; set; }
    }

    public class DisputePenaltyDto
    {
        [Range(0, 100000)]
        public decimal? FineAmount { get; set; }

        [Range(-100, 100)]
        public int? ScoreAdjustment { get; set; }
    }

    public class DisputePenaltySummaryDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }

        public decimal? FineAmount { get; set; }
        public string? FineStatus { get; set; }  //Paid | Unpaid | Voided
        public int? ScoreAdjustment { get; set; }
    }


    //Full dispute detail — shown on the dispute page
    public class DisputeDto
    {
        public int Id { get; set; }
        public int LoanId { get; set; }
        public int ItemId { get; set; } 
        public string ItemTitle { get; set; } = string.Empty;

        public bool IsMine { get; set; } //Contextual to the caller
        public string FiledById { get; set; } = string.Empty;
        public string FiledByName { get; set; } = string.Empty;
        public string FiledByUserName { get; set; } = string.Empty;
        public string? FiledByAvatarUrl { get; set; }
        public DisputeFiledAs FiledAs { get; set; } //owner or Borrower
        public string Description { get; set; } = string.Empty;
        public List<DisputePhotoDto> FiledByPhotos { get; set; } = new();

        public string? RespondedById { get; set; }
        public string? RespondedByName { get; set; }
        public string? RespondedByUserName { get; set; }
        public string? RespondedByAvatarUrl { get; set; }
        public string? ResponseDescription { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime ResponseDeadline { get; set; } //72h window
        public List<DisputePhotoDto> ResponsePhotos { get; set; } = new();

        public DisputeStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }

        public DisputeVerdict? AdminVerdict { get; set; }
        public string? AdminNote { get; set; } //Public explanation from admin
        public string? ResolvedByAdminId { get; set; }
        public string? ResolvedByAdminName { get; set; }
        public string? ResolvedByAdminUserName { get; set; }
        public string? ResolvedByAdminAvatarUrl { get; set; }


        //For frontend
        public bool CanEdit { get; set; }
        public bool CanCancel { get; set; }
        public bool CanRespond { get; set; }
        public bool CanAddEvidence { get; set; }

        public bool CanAddResponseEvidence { get; set; }
        public List<DisputePenaltySummaryDto> Penalties { get; set; } = new();

        public ItemCondition? SnapshotCondition { get; set; } //Item state at pickup
        public List<LoanSnapshotPhotoDto> SnapshotPhotos { get; set; } = new();
    }

    //Compact dispute — used in admin queue and user dispute list
    public class DisputeListDto
    {
        public int Id { get; set; }
        public int LoanId { get; set; }
        public string ItemTitle { get; set; } = string.Empty;

        public string FiledById { get; set; } = string.Empty;
        public string FiledByName { get; set; } = string.Empty;
        public string FiledByUsername { get; set; } = string.Empty;
        public string? FiledByAvatarUrl { get; set; }
        public DisputeFiledAs FiledAs { get; set; }

        public string? OtherPartyName { get; set; } //The Respondent
        public string? OtherPartyUserName { get; set; } //The Respondent
        public string? OtherPartyAvatarUrl { get; set; } 

        public DisputeStatus Status { get; set; }
        public bool HasResponse { get; set; } 
        public bool IsOverDue { get; set; }

        public DateTime ResponseDeadline { get; set; }
        public DateTime CreatedAt { get; set; }
    }


    public class DisputeStatsDto
    {
        public int TotalOpen { get; set; }
        public int AwaitingResponse { get; set; }
        public int UnderReview { get; set; }
        public int OverdueResponse { get; set; }   //past 72h, no reply yet
        public int ResolvedThisMonth { get; set; }
        public Dictionary<DisputeVerdict, int> VerdictBreakdown { get; set; } = new();
    }



}