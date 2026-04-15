using backend.Dtos;

namespace backend.Dtos
{
    public class RecentlyViewedDto
    {
        public class RecentlyViewedResponseDto
        {
            public ItemDto Item { get; set; } = null!;
            public DateTime ViewedAt { get; set; }
        }
    }
}