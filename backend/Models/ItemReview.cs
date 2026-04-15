using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class ItemReview
    {
        public int Id { get; set; }

        //The item being reviewed
        public int ItemId { get; set; }
        public Item Item { get; set; } = null!;

        //Tied to a specific loan — proves that the reviewer actually borrowed this item
        //One review per loan maximum
        public int? LoanId { get; set; } //no need loanid for admin
        public Loan? Loan { get; set; }

        //The borrower who is leaving the review
        public string ReviewerId { get; set; } = string.Empty;
        public ApplicationUser Reviewer { get; set; } = null!;

        //If admin reviews an item
        public bool IsAdminReview { get; set; } = false;


        public bool IsEdited { get; set; } = false;
        public DateTime? EditedAt { get; set; }


        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByAdminId { get; set; }


        [Range(1, 5)]
        public int Rating { get; set; } //1–5 stars
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
