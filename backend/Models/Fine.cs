
namespace backend.Models
{
    public class Fine
    {
        public int Id { get; set; }

        //Loan info
        public int? LoanId { get; set; }
        public Loan? Loan { get; set; }

        //The user who owns the fine
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public string? AdminNote { get; set; } //Set when admin manually issues a fine
        public FineType Type { get; set; } //Type of fine

        //Fine status and desc
        public FineStatus Status { get; set; } = FineStatus.Unpaid;
        public string? PaymentProofImageUrl { get; set; }
        public string? PaymentDescription { get; set; }
        public string? RejectionReason { get; set; }

        public decimal Amount { get; set; }
        public decimal? ItemValueAtTimeOfFine { get; set; }

        public PaymentMethod? PaymentMethod { get; set; }  //how the user claims they paid
        public DateTime? ProofSubmittedAt { get; set; }//When user submitted proof
        public DateTime? PaidAt { get; set; }  //When admin confirmed payment

        public DateTime? VoidAt { get; set; }


        public string? IssuedByAdminId { get; set; }
        public ApplicationUser? IssuedByAdmin {  get; set; }
        public string? VerifiedByAdminId { get; set; }  //who confirmed the payment
        public ApplicationUser? VerifiedByAdmin { get; set; }


        //Dispute link - If this fine was created as the result of a dispute verdict,
        //Linked it here so we can trace it 
        public int? DisputeId { get; set; }
        public Dispute? Dispute { get; set; }


        public Appeal? Appeal { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
