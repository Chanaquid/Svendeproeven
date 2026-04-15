
using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class Loan
    {
        public int Id { get; set; }

        //Item info - it has owner info in it 
        public int ItemId { get; set; }
        public Item Item { get; set; } = null!;


        public string LenderId { get; set; } = string.Empty; 
        public ApplicationUser Lender { get; set; } = null!;

        //Borrower Info
        public string BorrowerId { get; set; } = string.Empty;
        public ApplicationUser Borrower { get; set; } = null!;

        [MaxLength(500)]
        public string? NoteToOwner { get; set; }
        //DAtes
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; } //Capped at Item.AvailableUntil

        public DateTime? ActualReturnDate { get; set; }


        //Extension Date - one extension per loan max
        public DateTime? RequestedExtensionDate { get; set; } //New end date requested by borrower
        public ExtensionStatus? ExtensionRequestStatus { get; set; } //Pending/Approved/Rejected


        //pricing — snapshotted at booking time, Item.PricePerDay may change later
        public decimal TotalPrice { get; set; }

        //QR scan timestamps
        public DateTime? PickedUpAt { get; set; } //borrower scanned QR on pickup
        public DateTime? ReturnedAt { get; set; } //borrower scanned QR on return



        //Status
        public LoanStatus Status { get; set; } = LoanStatus.Pending;
        public ItemCondition SnapshotCondition { get; set; } //item condition at loan start
        public decimal PricePerDaySnapshot { get; set; }


        //Admin review -> if score is less than 50
        public string? AdminReviewerId { get; set; }
        public ApplicationUser? AdminReviewer { get; set; }
        public DateTime? AdminReviewedAt { get; set; }

        //Owner decision
        public string? OwnerApproverId { get; set; }
        public ApplicationUser? OwnerApprover { get; set; }
        public DateTime? OwnerApprovedAt { get; set; }

        public DateTime? DisputeDeadline { get; set; }

        public string? DecisionNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }


        //Navigation
        public ICollection<LoanSnapshotPhoto> SnapshotPhotos { get; set; } = new List<LoanSnapshotPhoto>();
        public ICollection<Fine> Fines { get; set; } = new List<Fine>(); //One loan can have multiple fines like late + damaged/lost
        public ICollection<LoanMessage> Messages { get; set; } = new List<LoanMessage>(); //All messages are scoped to a loan
        public ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();




    }
}
