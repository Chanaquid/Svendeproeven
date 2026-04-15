namespace backend.Dtos
{
    public class UserRecentlyViewedItemDto
    {
        public int ItemId { get; set; }
        public string ItemTitle { get; set; } = string.Empty;
        public string ItemSlug { get; set; } = string.Empty;
        public string? ItemMainPhotoUrl { get; set; }
        public decimal PricePerDay { get; set; }
        public bool IsFree { get; set; }
        public bool IsAvailable { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public DateTime ViewedAt { get; set; }
    }
}
