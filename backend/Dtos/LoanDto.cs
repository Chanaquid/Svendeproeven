using backend.Dtos;
using backend.Models;
using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{

    //Borrower requests a loan on an item
    public class CreateLoanDto
    {
        [Required]
        public int ItemId { get; set; }

        [MaxLength(500)]
        public string? NoteToOwner { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; } //Service validates this is not beyond Item.AvailableUntil
    }

    public class UpdateLoanDatesDto
    {
        [Required(ErrorMessage = "Loan ID is required")]
        public int LoanId { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        public DateTime EndDate { get; set; }
    }

    //Owner decides a loan request
    public class OwnerDecideLoanDto
    {
        [Required(ErrorMessage = "Loan ID is required")]
        public int LoanId { get; set; }

        [Required(ErrorMessage = "Approval status is required")]
        public bool IsApproved { get; set; }

        [MaxLength(1000, ErrorMessage = "Decision note cannot exceed 1000 characters")]
        public string? DecisionNote { get; set; }
    }

    //If vorrowers score is low
    public class AdminReviewLoanDto
    {
        [Required(ErrorMessage = "Loan ID is required")]
        public int LoanId { get; set; }

        [Required(ErrorMessage = "Approval status is required")]
        public bool IsApproved { get; set; }

        [MaxLength(1000, ErrorMessage = "Admin note cannot exceed 1000 characters")]
        public string? AdminNote { get; set; }
    }

    //Borrower requests extension
    public class RequestExtensionDto
    {
        [Required(ErrorMessage = "Loan ID is required")]
        public int LoanId { get; set; }

        [Required(ErrorMessage = "New end date is required")]
        public DateTime RequestedExtensionDate { get; set; }
    }

    //Owner approves or rejects extension
    public class DecideExtensionDto
    {
        [Required]
        public int LoanId { get; set; }

        [Required]
        public bool IsApproved { get; set; }

        [MaxLength(500, ErrorMessage = "Note cannot exceed 500 characters")]
        public string? Note { get; set; }
    }

    //Fpr pickup or return
    public class ScanQrCodeDto
    {
        [Required(ErrorMessage = "QR code is required")]
        public string QrCode { get; set; } = string.Empty;
    }


    //Borrower cancels their own pending/approved loan before pickup
    public class CancelLoanDto
    {
        [Required]
        public int LoanId { get; set; }

        [MaxLength(1000)]
        public string? Reason { get; set; }
    }

    public class LoanDto
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string ItemTitle { get; set; } = string.Empty;
        public string ItemSlug { get; set; } = string.Empty;
        public string? ItemMainPhotoUrl { get; set; }

        //Owner
        public string LenderId { get; set; } = string.Empty;
        public string LenderName { get; set; } = string.Empty;
        public string LenderUserName { get; set; } = string.Empty;
        public string? LenderAvatarUrl { get; set; }
        public int LenderScore { get; set; }

        //Borrower
        public string BorrowerId { get; set; } = string.Empty;
        public string BorrowerName { get; set; } = string.Empty;
        public string BorrowerUserName { get; set; } = string.Empty;
        public string? BorrowerAvatarUrl { get; set; }
        public int BorrowerScore { get; set; }
        public string? NoteToOwner { get; set; }

        //Dates
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? ActualReturnDate { get; set; }

        //Extension
        public DateTime? RequestedExtensionDate { get; set; }
        public ExtensionStatus? ExtensionRequestStatus { get; set; }

        public decimal TotalPrice { get; set; }

        //QR scan timestamps
        public DateTime? PickedUpAt { get; set; }
        public DateTime? ReturnedAt { get; set; }

        public LoanStatus Status { get; set; }
        public ItemCondition SnapshotCondition { get; set; }

        //Admin review
        public string? AdminReviewerId { get; set; }
        public string? AdminReviewerName { get; set; }
        public string? AdminReviewerUserName { get; set; }
        public string? AdminReviewerAvatarUrl { get; set; }
        public DateTime? AdminReviewedAt { get; set; }

        //Owners decision
        public DateTime? OwnerApprovedAt { get; set; }
        public string? DecisionNote { get; set; }

        //Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        
        //Computed properties (set in service)
        public bool IsOverdue { get; set; }
        public bool CanBeExtended { get; set; }
        public bool IsMine { get; set; } //Current user is borrower
        public bool IsMyItem { get; set; } //Current user is lender/owner
        public bool CanReview { get; set; }
        public bool HasReviewed { get; set; }

        //Related data
        public List<DisputeDto> Disputes { get; set; } = new();
        public List<LoanMessageDto> Messages { get; set; } = new();
        public List<FineDto> Fines { get; set; } = new();
        public List<LoanSnapshotPhotoDto> SnapshotPhotos { get; set; } = new();
    }

    //Compact loan — used in user's loan list as borrower or owner
    public class LoanListDto
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string ItemTitle { get; set; } = string.Empty;
        public string? ItemMainPhotoUrl { get; set; }

        public string OtherPartyId { get; set; } = string.Empty;
        public string OtherPartyName { get; set; } = string.Empty;
        public string OtherPartyUserName { get; set; } = string.Empty;
        public string? OtherPartyAvatarUrl { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? ActualReturnDate { get; set; }

        public LoanStatus Status { get; set; }
        public decimal TotalPrice { get; set; }

        public bool IsOverdue => !ActualReturnDate.HasValue && DateTime.UtcNow > EndDate;
        public bool IsBorrower { get; set; } //Current user is borrower

        public bool HasDispute { get; set; }
        public DateTime CreatedAt { get; set; }

    }


    //Admin pending loan queue — low score users waiting for admin approval
    public class AdminPendingLoanDto
    {
        public int Id { get; set; }
        public string ItemTitle { get; set; } = string.Empty;
        public string? ItemPrimaryPhoto { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerUserName { get; set; } = string.Empty;
        public string BorrowerName { get; set; } = string.Empty;
        public string BorrowerUserName { get; set; } = string.Empty;
        public string BorrowerEmail { get; set; } = string.Empty;
        public string? BorrowerAvatarUrl { get; set; }
        public int BorrowerScore { get; set; }
        public decimal BorrowerUnpaidFines { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    
}