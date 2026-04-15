using backend.Models;
using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{
    //Admin manually adjusts user score
    public class AdminAdjustScoreDto
    {
        [Required(ErrorMessage = "User ID is required")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public ScoreChangeReason Reason { get; set; }
        public int? LoanId { get; set; } //if its an outcome from a dispute from a loan

        [Range(-100, 100, ErrorMessage = "Points must be between -100 and 100")] 
        public int PointsChanged { get; set; } //Can be positive or negative

        [MaxLength(500, ErrorMessage = "Note cannot exceed 500 characters")]
        public string? Note { get; set; }
    }

    //Lightweight list view
    public class ScoreHistoryDto
    {
        public int Id { get; set; }
        public int PointsChanged { get; set; }
        public int ScoreAfterChange { get; set; }
        public ScoreChangeReason Reason { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    //User score summary
    public class UserScoreSummaryDto
    {
        public int CurrentScore { get; set; }
        public int TotalPointsEarned { get; set; }
        public int TotalPointsLost { get; set; }
        public int TotalScoreEvents { get; set; }
        public DateTime? LastScoreChangeAt { get; set; }
    }






}
