using System.ComponentModel.DataAnnotations;

namespace backend.DTOs
{
        //Requests

        //Borrower leaves a review for the item after loan is completed
        public class CreateItemReviewDto
        {
            public int? LoanId { get; set; } //Optional for admin

            [Required]
            public int ItemId { get; set; }

            [Required, Range(1, 5)]
            public int Rating { get; set; }

            [MaxLength(1000)]
            public string? Comment { get; set; }
        }

        //Either party leaves a review for the other user after loan is completed
        public class CreateUserReviewDto
        {
            public int? LoanId { get; set; } //Optional for admin

            [Required]
            public string ReviewedUserId { get; set; } = string.Empty;

            [Required, Range(1, 5)]
            public int Rating { get; set; }

            [MaxLength(1000)]
            public string? Comment { get; set; }
        }

        //Admin edits a review
        public class EditReviewDto
        {
            [Required, Range(1, 5)]
            public int Rating { get; set; }

            [MaxLength(1000)]
            public string? Comment { get; set; }
        }

        //Responses

        //Item review — shown on item detail page
        public class ItemReviewResponseDto
        {
            public int Id { get; set; }
            public int? LoanId { get; set; }
            public string ReviewerId { get; set; } = string.Empty;
            public string ReviewerName { get; set; } = string.Empty;
            public string ReviewerUserName { get; set; } = string.Empty;
             public string? ReviewerAvatarUrl { get; set; }
            public int Rating { get; set; }
            public string? Comment { get; set; }
            public bool IsAdminReview { get; set; }
            public bool IsEdited { get; set; }
            public DateTime? EditedAt { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        //User review — shown on a user's profile page
        public class UserReviewResponseDto
        {
            public int Id { get; set; }
            public int? LoanId { get; set; }
            public string? ItemTitle { get; set; } //Context for what loan this review is from
            public string ReviewerId { get; set; } = string.Empty;
            public string ReviewerName { get; set; } = string.Empty;
            public string ReviewerUserName { get; set; } = string.Empty;
        public string? ReviewerAvatarUrl { get; set; }
            public int Rating { get; set; }
            public string? Comment { get; set; }
            public bool IsAdminReview { get; set; }
            public bool IsEdited { get; set; }
            public DateTime? EditedAt { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    
}