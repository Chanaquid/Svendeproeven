using backend.Models;

namespace backend.Dtos
{
    public class UserFilter
    {
        public bool? ExcludeAdmins { get; set; }
        public bool? IncludeDeleted { get; set; }
        public bool? IsPermanentBan { get; set; }
        public bool? IsBanned { get; set; } //true = banned, false = not banned
        public bool? IsDeleted { get; set; } //true = deleted, false = not deleted
        public bool? IsVerified { get; set; } //true = verified, false = unverified
        public string? Role { get; set; } //"Admin" or "User"
        public decimal? MinScore { get; set; } //Low score threshold
        public decimal? MaxScore { get; set; } //High score threshold
        public bool? HasUnpaidFines { get; set; } //True = only users with unpaid fines


        //Location filter
        public double? Latitude { get; set; } //center latitude
        public double? Longitude { get; set; }//center longitude
        public double? RadiusKm { get; set; }//Tadius in kilometers

        //text search
        public string? Search { get; set; }
    }

    public class ItemFilter
    {
        //Mains
        public bool? IsFree { get; set; }
        public bool? RequiresVerification { get; set; }
        public ItemAvailability? Availability { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? RadiusKm { get; set; }

        public double? MinRating { get; set; }
        public double? MaxRating { get; set; }

        public string? Search { get; set; }

        //mid
        public ItemCondition? Condition { get; set; }
        public DateTime? AvailableFrom { get; set; }
        public DateTime? AvailableUntil { get; set; }
        public int? CategoryId { get; set; }
        public string? OwnerId { get; set; }

        public int? MaxLoanDays { get; set; }
        public int? MinLoanDays { get; set; }
        public DateTime? CreatedAfter { get; set; }


        //Admins
        public ItemStatus? Status { get; set; }
        public bool? IsActive { get; set; }
    }

    public class ItemReviewFilter
    {
        //Filter by rating (1–5 stars)
        public int? MinRating { get; set; }
        public int? MaxRating { get; set; }

        //show only verified reviews 
        public bool? IsVerifiedReviewer { get; set; }

        //Optional search in review text
        public string? Search { get; set; }

        //Filter by date range
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        //only reviews with text (not just star rating)
        public bool? HasComment { get; set; }
    }

    public class LoanFilter
    {
        //Identity
        public string? BorrowerId { get; set; }
        public string? LenderId { get; set; }
        public int? ItemId { get; set; }

        //Statuses
        public LoanStatus? Status { get; set; }
        public ExtensionStatus? ExtensionRequestStatus { get; set; }
        public bool? IsOverdue { get; set; }

        //Date Ranges
        public DateTime? CreatedAfter { get; set; }
        public DateTime? StartsAfter { get; set; }
        public DateTime? EndsBefore { get; set; }

        //Flags for Admin/Management
        public bool? HasFines { get; set; }
        public bool? HasDisputes { get; set; }
        public bool? HasMessages { get; set; }

        //Search
        public string? Search { get; set; }
    }

    public class NotificationFilter
    {
        public NotificationType? Type { get; set; }
        public NotificationReferenceType? ReferenceType { get; set; }
        public bool? IsRead { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public int? ReferenceId { get; set; }
        public string? Search { get; set; }


    }

    public class AppealFilter
    {
        public string? UserId { get; set; }
        public string? ResolvedByAdminId { get; set; }

        //Types & Status
        public AppealType? AppealType { get; set; }
        public AppealStatus? Status { get; set; }

        //Related entities
        public int? FineId { get; set; }
        public int? ScoreHistoryId { get; set; }

        //Date filters
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public DateTime? ResolvedAfter { get; set; }
        public DateTime? ResolvedBefore { get; set; }

        //Flags
        public bool? IsResolved { get; set; } //shortcut for Status != Pending

        //Search
        public string? Search { get; set; } //Message, AdminNote
    }

    public class DisputeFilter
    {
        public string? FiledById { get; set; }
        public string? RespondedById { get; set; }
        public string? ResolvedByAdminId { get; set; }

        public int? LoanId { get; set; }

        //Roles
        public DisputeFiledAs? FiledAs { get; set; }

        //Status & verdict
        public DisputeStatus? Status { get; set; }
        public DisputeVerdict? AdminVerdict { get; set; }

        //Flags
        public bool? IsResolved { get; set; }
        public bool? HasResponse { get; set; }
        public bool? IsOverdueResponse { get; set; }

        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public DateTime? ResolvedAfter { get; set; }
        public DateTime? ResolvedBefore { get; set; }
        public DateTime? ResponseDeadlineBefore { get; set; }
        public DateTime? ResponseDeadlineAfter { get; set; }

        //Financial
        public decimal? MinCustomFine { get; set; }
        public decimal? MaxCustomFine { get; set; }

        //Search
        public string? Search { get; set; }
    }

    public class FineFilter
    {
        public string? UserId { get; set; }
        public string? IssuedByAdminId { get; set; }
        public FineStatus? Status { get; set; }
        public FineType? Type { get; set; }

        //Financial ranges
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }

        //Date ranges
        public DateTime? CreatedAfter { get; set; }
        public DateTime? PaidAfter { get; set; }

        public int? LoanId { get; set; }
        public int? DisputeId { get; set; }
        public bool? HasPaymentProof { get; set; }

        public string? Search { get; set; } //Searches AdminNote or PaymentDescription
    }

    public class ReportFilter
    {
        public string? ReportedById { get; set; }
        public string? HandledByAdminId { get; set; }
        public ReportType? Type { get; set; }
        public ReportReason? Reasons { get; set; }
        public ReportStatus? Status { get; set; }
        public string? TargetId { get; set; }
        public bool? IsResolved { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public DateTime? ResolvedAfter { get; set; }
        public DateTime? ResolvedBefore { get; set; }
        public string? Search { get; set; } //Search in AdditionalDetails or AdminNote


    }

    public class VerificationRequestFilter
    {
        public string? UserId { get; set; }
        public string? ReviewedByAdminId { get; set; }

        //tatus & type
        public VerificationStatus? Status { get; set; }
        public VerificationDocumentType? DocumentType { get; set; }

        //Flags
        public bool? IsReviewed { get; set; } //Status != Pending
        public bool? IsApproved { get; set; }
        public bool? IsRejected { get; set; }

        //Date filters
        public DateTime? SubmittedAfter { get; set; }
        public DateTime? SubmittedBefore { get; set; }
        public DateTime? ReviewedAfter { get; set; }
        public DateTime? ReviewedBefore { get; set; }

        //Search
        public string? Search { get; set; } //adminNote
    }

    public class ScoreHistoryFilter
    {
        public string? UserId { get; set; }
        public ScoreChangeReason? Reason { get; set; }
        public int? LoanId { get; set; }
        public int? DisputeId { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public bool? OnlyPositive { get; set; }
        public bool? OnlyNegative { get; set; }

        //Search
        public string? Search { get; set; } //Search in note
    }

    public class SupportThreadFilter
    {
        public string? UserId { get; set; }
        public string? ClaimedByAdminId { get; set; }

        public SupportThreadStatus? Status { get; set; }

        //Flags
        public bool? IsClaimed { get; set; } //ClaimedByAdminId != null
        public bool? IsUnclaimed { get; set; }// ClaimedByAdminId == null

        //Date filters
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public DateTime? ClaimedAfter { get; set; }
        public DateTime? ClaimedBefore { get; set; }
        public DateTime? ClosedAfter { get; set; }
        public DateTime? ClosedBefore { get; set; }

        // Search
        public string? Search { get; set; }
    }

    public class MessageFilter
    {
        //Search
        public string? Search { get; set; } //Search in message content

        //Date filters
        public DateTime? SentAfter { get; set; }
        public DateTime? SentBefore { get; set; }

        //Read status
        public bool? IsRead { get; set; } //true = read, false = unread, null = all

        //Sender filter
        public string? SenderId { get; set; } //Filter by specific sender

        //For admin
        public bool? IncludeDeleted { get; set; } // Include soft-deleted messages

    }

    public class ConversationFilter
    {
        public string? Search { get; set; } //Search by other user's name
        public DateTime? LastMessageAfter { get; set; }
        public DateTime? LastMessageBefore { get; set; }
        public bool? HasUnreadMessages { get; set; } //Filter conversations with unread messages
        public bool? IncludeHidden { get; set; } = false; //Include hidden conversations
    }

    public class UserBanHistoryFilter
    {
        public string? UserId { get; set; }
        public string? AdminId { get; set; }
        public bool? IsBanned { get; set; }  // true = bans only, false = unbans only
        public bool? IsPermanent { get; set; }
        public DateTime? BannedAfter { get; set; }
        public DateTime? BannedBefore { get; set; }

        public string? Search { get; set; }
    }


    public class UserBlockFilter
    {
        public string? BlockerId { get; set; }
        public string? BlockedId { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }

        public string? Search { get; set; }
    }


    public class UserReviewFilter
    {
        public string? ReviewerId { get; set; }
        public string? ReviewedUserId { get; set; }
        public int? LoanId { get; set; }
        public int? MinRating { get; set; }
        public int? MaxRating { get; set; }
        public bool? IsAdminReview { get; set; }
        public bool? IsEdited { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public string? Search { get; set; } // searches Comment
    }


    public class UserFavoriteItemFilter
    {
        public int? ItemId { get; set; }
        public bool? NotifyWhenAvailable { get; set; }
        public DateTime? SavedAfter { get; set; }
        public DateTime? SavedBefore { get; set; }
        public bool? OnlyAvailable { get; set; } //Filter to only show available items
        public string? Search { get; set; } //Search in item title

    }





}