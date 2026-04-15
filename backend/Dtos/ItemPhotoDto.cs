using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{
    public class ItemPhotoDto
    {
        public int Id { get; set; }

        public string PhotoUrl { get; set; } = string.Empty;

        public bool IsPrimary { get; set; } = false;

        public int DisplayOrder { get; set; } = 0;

        public DateTime UploadedAt { get; set; }
    }

    public class AddItemPhotoDto
    {
        [Required]
        public string PhotoUrl { get; set; } = string.Empty;
        public bool IsPrimary { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdateItemPhotoDto
    {
        [Required]
        public int Id { get; set; }
        public bool? IsPrimary { get; set; }
        public int? DisplayOrder { get; set; }
    }

    public class ReorderItemPhotosDto
    {
        [Required]
        public int ItemId { get; set; }

        [Required]
        public List<int> PhotoIdsInOrder { get; set; } = new();
    }

    public class SetPrimaryPhotoDto
    {
        [Required]
        public int PhotoId { get; set; }

        [Required]
        public int ItemId { get; set; }
    }

    public class PhotoOrderUpdateDto
    {
        [Required]
        public int PhotoId { get; set; }

        [Required]
        public int DisplayOrder { get; set; }
    }

    public class DeleteItemPhotoDto
    {
        [Required(ErrorMessage = "Photo ID is required")]
        public int PhotoId { get; set; }

        [Required(ErrorMessage = "Item ID is required")]
        public int ItemId { get; set; }
    }

}
