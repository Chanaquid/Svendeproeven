using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{

    //Admin creating a category
    public class CreateCategoryDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

   
        [MaxLength(10)]
        public string? Icon { get; set; }
    }

    //Admin updates an existing category
    public class UpdateCategoryDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;


        [MaxLength(10)]
        public string? Icon { get; set; }

        public bool IsActive { get; set; }
    }

    //Responses
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public bool IsActive { get; set; }
        public int ItemCount { get; set; } //Approved + active items in this category
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    //For listing categories in a simplified way (e.g., dropdown)
    public class CategoryListItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string Slug { get; set; } = string.Empty;
        public int ItemCount { get; set; }

    }

    
}