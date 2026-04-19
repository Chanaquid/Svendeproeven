using backend.Models;
using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{

    public class FavoriteToggleResultDto
    {
        public int ItemId { get; set; }
        public bool IsFavorited { get; set; }
    }

    public class FavoriteStatusDto
    {
        public int ItemId { get; set; }
        public bool IsFavorited { get; set; }
    }

    public class NotifyPreferenceResultDto
    {
        public int ItemId { get; set; }
        public bool NotifyWhenAvailable { get; set; }
    }

    public class UpdateNotifyPreferenceDto
    {
        [Required]
        public bool Notify { get; set; }
    }


    public class UserFavoriteItemListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public string Slug { get; set; } = string.Empty;
        public string? MainPhotoUrl { get; set; }
        public string PickUpAddress { get; set; } = string.Empty;

        //Category
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategorySlug { get; set; } = string.Empty;
        public string? CategoryIcon { get; set; }


        //Owner
        public string OwnerId { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerUsername { get; set; } = string.Empty;
        public string OwnerAvatarUrl { get; set; } = string.Empty;
        public int OwnerScore { get; set; }
        public bool IsOwnerVerified { get; set; }


        public bool IsFree { get; set; }
        public decimal PricePerDay { get; set; }
        public ItemCondition Condition { get; set; }
        //public ItemStatus Status { get; set; }
        public ItemAvailability Availability { get; set; }
        public bool IsActive { get; set; }
        //public bool IsCurrentlyOnLoan { get; set; }

        public double? AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int? MaxLoanDays { get; set; }
        public int? MinLoanDays { get; set; }

        public DateTime AvailableFrom { get; set; }
        public DateTime AvailableUntil { get; set; }
        public double? DistanceFromUser { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool NotifyWhenAvailable { get; set; }
        public DateTime SavedAt { get; set; }
    }

}
