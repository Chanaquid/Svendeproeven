using backend.Models;
using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{
    public class CreateItemDto : IValidatableObject
    {
        [Required]
        public int CategoryId { get; set; }

        [Required, MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0, 100000000)]
        public decimal CurrentValue { get; set; }

        [Range(0, 100000)]
        public decimal PricePerDay { get; set; }

        public bool IsFree { get; set; } = false;

        [Required]
        public ItemCondition Condition { get; set; }

        public int? MinLoanDays { get; set; }
        public int? MaxLoanDays { get; set; }
        public bool RequiresVerification { get; set; } = false;

        [Required, MaxLength(500)]
        public string PickupAddress { get; set; } = string.Empty;

        [Required]
        [Range(-90, 90, ErrorMessage = "Pickup latitude must be between -90 and 90")]
        public double PickupLatitude { get; set; }

        [Required]
        [Range(-180, 180, ErrorMessage = "Pickup longitude must be between -180 and 180")]
        public double PickupLongitude { get; set; }

        [Required]
        public DateTime AvailableFrom { get; set; }

        [Required]
        public DateTime AvailableUntil { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (IsFree && PricePerDay > 0)
                yield return new ValidationResult(
                    "PricePerDay must be 0 when IsFree is true.",
                    new[] { nameof(PricePerDay) });

            if (!IsFree && PricePerDay <= 0)
                yield return new ValidationResult(
                    "PricePerDay must be greater than 0 for paid items.",
                    new[] { nameof(PricePerDay) });

            if (AvailableUntil <= AvailableFrom)
                yield return new ValidationResult(
                    "AvailableUntil must be after AvailableFrom.",
                    new[] { nameof(AvailableUntil) });

            if (MinLoanDays.HasValue && MaxLoanDays.HasValue && MinLoanDays > MaxLoanDays)
                yield return new ValidationResult(
                    "MinLoanDays cannot be greater than MaxLoanDays.",
                    new[] { nameof(MinLoanDays) });
        }
    }

    public class UpdateItemDto : IValidatableObject
    {
        [MaxLength(100)]
        public string? Title { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        public int? CategoryId { get; set; }

        [Range(0, 100000000)]
        public decimal? CurrentValue { get; set; }

        [Range(0, 100000)]
        public decimal? PricePerDay { get; set; }

        public bool? IsFree { get; set; }
        public ItemCondition? Condition { get; set; }
        public int? MinLoanDays { get; set; }
        public int? MaxLoanDays { get; set; }
        public bool? RequiresVerification { get; set; }

        [MaxLength(500)]
        public string? PickupAddress { get; set; }

        [Range(-90, 90)]
        public double? PickupLatitude { get; set; }

        [Range(-180, 180)]
        public double? PickupLongitude { get; set; }

        public DateTime? AvailableFrom { get; set; }
        public DateTime? AvailableUntil { get; set; }
        public bool? IsActive { get; set; }
        public ItemAvailability? Availability { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (IsFree == true && PricePerDay > 0)
                yield return new ValidationResult(
                    "PricePerDay must be 0 when IsFree is true.",
                    new[] { nameof(PricePerDay) });

            if (IsFree == false && PricePerDay <= 0)
                yield return new ValidationResult(
                    "PricePerDay must be greater than 0 for paid items.",
                    new[] { nameof(PricePerDay) });

            if (AvailableFrom.HasValue && AvailableUntil.HasValue && AvailableUntil <= AvailableFrom)
                yield return new ValidationResult(
                    "AvailableUntil must be after AvailableFrom.",
                    new[] { nameof(AvailableUntil) });

            if (MinLoanDays.HasValue && MaxLoanDays.HasValue && MinLoanDays > MaxLoanDays)
                yield return new ValidationResult(
                    "MinLoanDays cannot be greater than MaxLoanDays.",
                    new[] { nameof(MinLoanDays) });

            //Partial coordinate update — must provide both or neither
            if (PickupLatitude.HasValue != PickupLongitude.HasValue)
                yield return new ValidationResult(
                    "PickupLatitude and PickupLongitude must both be provided together.",
                    new[] { nameof(PickupLatitude), nameof(PickupLongitude) });
        }
    }

    public class AdminDecideItemDto
    {
        [Required]
        public bool IsApproved { get; set; }

        [MaxLength(1000)]
        public string? AdminNote { get; set; }
    }

    public class AdminUpdateItemStatusDto
    {
        [Required]
        public ItemStatus Status { get; set; }

        [MaxLength(1000)]
        public string? AdminNote { get; set; }
    }

    public class ToggleActiveStatusDto
    {
        [Required]
        public bool IsActive { get; set; }
    }

    //Full item detail — shown on item detail page
    public class ItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public decimal PricePerDay { get; set; }
        public bool IsFree { get; set; }
        public ItemCondition Condition { get; set; }
        public bool IsCurrentlyOnLoan { get; set; }
        public bool IsMine { get; set; }

        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategorySlug { get; set; } = string.Empty;
        public string? CategoryIcon { get; set; }

        public string OwnerId { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerUserName { get; set; } = string.Empty;
        public string? OwnerAvatarUrl { get; set; }
        public int OwnerScore { get; set; }
        public bool IsOwnerVerified { get; set; }

        public int? MinLoanDays { get; set; }
        public int? MaxLoanDays { get; set; }
        public bool RequiresVerification { get; set; }

        public string PickupAddress { get; set; } = string.Empty;
        public double PickupLatitude { get; set; }
        public double PickupLongitude { get; set; }

        public DateTime AvailableFrom { get; set; }
        public DateTime AvailableUntil { get; set; }

        public ItemStatus Status { get; set; }
        public ItemAvailability Availability { get; set; }
        public bool IsActive { get; set; }

        public string? AdminNote { get; set; }
        public string? ReviewedByAdminId { get; set; }
        public string? ReviewedByAdminName { get; set; }
        public string? ReviewedByAdminUserName { get; set; }
        public string? ReviewedByAdminAvatarUrl { get; set; }
        public DateTime? ReviewedAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public double? AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public int TotalLoans { get; set; }
        public bool IsFavoritedByCurrentUser { get; set; }

        public List<ItemPhotoDto> Photos { get; set; } = new();
    }

    //Browse grid
    public class ItemListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public string Slug { get; set; } = string.Empty;
        public string? MainPhotoUrl { get; set; }
        public string PickupAddress { get; set; } = string.Empty;

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

        public ItemStatus Status { get; set; }
        public bool IsFree { get; set; }
        public decimal PricePerDay { get; set; }
        public ItemCondition Condition { get; set; }
        //public ItemStatus Status { get; set; }
        public ItemAvailability Availability { get; set; }
        public bool IsActive { get; set; }
        //public bool IsCurrentlyOnLoan { get; set; }
        public bool RequiresVerification { get; set; }

        public double? AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int? MaxLoanDays { get; set; }
        public int? MinLoanDays { get; set; }

        public DateTime AvailableFrom { get; set; }
        public DateTime AvailableUntil { get; set; }
        public double? DistanceFromUser { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ItemQrCodeDto
    {
        public int ItemId { get; set; }
        public string QrCode { get; set; } = string.Empty;
    }
}