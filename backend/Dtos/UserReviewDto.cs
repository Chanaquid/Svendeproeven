using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{
    public class CreateUserReviewDto
    {
        [Required]
        public int LoanId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }

    public class UpdateUserReviewDto
    {
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }

    public class AdminCreateUserReviewDto
    {
        [Required]
        public string ReviewedUserId { get; set; } = string.Empty;

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [Required, MaxLength(1000)]
        public string Comment { get; set; } = string.Empty;
    }

    public class UserReviewDto
    {
        public int Id { get; set; }
        public int? LoanId { get; set; }
        public string ReviewerId { get; set; } = string.Empty;
        public string ReviewerName { get; set; } = string.Empty;
        public string ReviewerUserName { get; set; } = string.Empty;
        public string? ReviewerAvatarUrl { get; set; }
        public string ReviewedUserId { get; set; } = string.Empty;
        public string ReviewedFullName { get; set; } = string.Empty;
        public string ReviewedUserName { get; set; } = string.Empty;
        public string? ReviewedUserAvatarUrl { get; set; }
        public bool IsMine { get; set; }
        public bool IsAdminReview { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserReviewListDto
    {
        public int Id { get; set; }
        public string? ItemTitle { get; set; }
        public string ReviewerId { get; set; } = string.Empty;
        public string ReviewerName { get; set; } = string.Empty;
        public string ReviewerUserName { get; set; } = string.Empty;
        public string? ReviewerAvatarUrl { get; set; }
        public bool IsAdminReview { get; set; }
        public bool IsMine { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserRatingSummaryDto
    {
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int Rating1Count { get; set; }
        public int Rating2Count { get; set; }
        public int Rating3Count { get; set; }
        public int Rating4Count { get; set; }
        public int Rating5Count { get; set; }
    }



}
