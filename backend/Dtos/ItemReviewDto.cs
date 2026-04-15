using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{
    public class CreateItemReviewDto
    {
        [Required]
        public int ItemId { get; set; }

        //Required for regular users
        public int? LoanId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

    }

    public class UpdateItemReviewDto
    {
        [Required(ErrorMessage = "Review ID is required")]
        public int ReviewId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }

    public class AdminCreateItemReviewDto
    {
        [Required(ErrorMessage = "Item ID is required")]
        public int ItemId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [MaxLength(2000)]
        public string? Comment { get; set; }

        //public bool IsAdminReview { get; set; } = true;
    }

    public class ItemReviewDto
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public int? LoanId { get; set; }

        public string ReviewerId { get; set; } = string.Empty;
        public string ReviewerName { get; set; } = string.Empty;
        public string ReviewerUserName { get; set; } = string.Empty;
        public string? ReviewerAvatarUrl { get; set; }
        public bool IsMine { get; set; }

        public int Rating { get; set; }
        public string? Comment { get; set; }

        public bool IsAdminReview { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
        public DateTime CreatedAt { get; set; }

    }

    public class ItemReviewListDto
    {
        public int Id { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public string ReviewerUserName { get; set; } = string.Empty;
        public string? ReviewerAvatarUrl { get; set; }
        public bool IsAdminReview { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ItemRatingSummaryDto
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
