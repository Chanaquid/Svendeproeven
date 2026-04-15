using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{
    public class CreateDisputePhotoDto
    {
        [Required]
        public int DisputeId { get; set; }

        [Required]
        public string PhotoUrl { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Caption { get; set; }
    }

    public class AddDisputePhotoDto
    {
        [Required(ErrorMessage = "Photo URL is required")]
        [Url(ErrorMessage = "Invalid URL format")]
        [MaxLength(500, ErrorMessage = "Photo URL cannot exceed 500 characters")]
        public string PhotoUrl { get; set; } = string.Empty;

        [MaxLength(200, ErrorMessage = "Caption cannot exceed 200 characters")]
        public string? Caption { get; set; }
    }


    public class CreateMultipleDisputePhotosDto
    {
        [Required]
        public int DisputeId { get; set; }

        [Required]
        public List<string> PhotoUrls { get; set; } = new();

        public string? Caption { get; set; }
    }

    public class DisputePhotoDto
    {
        public int Id { get; set; }
        public int DisputeId { get; set; }

        public string SubmittedById { get; set; } = string.Empty;
        public string SubmittedByName { get; set; } = string.Empty;
        public string SubmittedByUserName { get; set; } = string.Empty;
        public string? SubmittedByAvatarUrl { get; set; }

        public string PhotoUrl { get; set; } = string.Empty;
        public string? Caption { get; set; }

        public DateTime UploadedAt { get; set; }

        public bool IsMine { get; set; }
    }

    public class DisputePhotoListDto
    {
        public int Id { get; set; }
        public string PhotoUrl { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class DeleteDisputePhotoDto
    {
        [Required(ErrorMessage = "Photo ID is required")]
        public int PhotoId { get; set; }

        [Required(ErrorMessage = "Dispute ID is required")]
        public int DisputeId { get; set; }
    }

}