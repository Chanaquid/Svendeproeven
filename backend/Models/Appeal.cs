
namespace backend.Models
{
    public class Appeal
    {
        public int Id { get; set; }

        //User info
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        //Score or Fine appeal
        public AppealType AppealType { get; set; }
        public string Message { get; set; } = string.Empty; //Since User's score is low, they can explain why they need to borrow it. (+ maybe an apology?)

        public AppealStatus Status { get; set; } = AppealStatus.Pending;

        //Only set for fine appeals
        public int? FineId { get; set; }
        public Fine? Fine { get; set; }

        //Fine appeal resolution — only set when Type == Fine and appeal is approved
        public FineAppealResolution? FineResolution { get; set; }

        //For fine appeal — how admin resolved it
        public decimal? CustomFineAmount { get; set; }


        //for score appeal — admin can manually set score (defaults to 20)
        public int? ScoreHistoryId { get; set; }
        public ScoreHistory? ScoreHistory { get; set; }
        public int? RestoredScore { get; set; }



        //Admins response
        public string? AdminNote { get; set; }
        //Admin info
        public string? ResolvedByAdminId { get; set; }
        public ApplicationUser? ResolvedByAdmin { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }



    }
}


