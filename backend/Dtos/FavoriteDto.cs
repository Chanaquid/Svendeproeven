namespace backend.Dtos
{
        //Response — returned when listing favorites
        public class FavoriteResponseDto
        {
            public ItemDto Item { get; set; } = null!;
            public bool NotifyWhenAvailable { get; set; }
            public DateTime SavedAt { get; set; }
        }

        //Request — PATCH /api/favorites/{itemId}/notify
        public class ToggleNotifyDto
        {
            public bool NotifyWhenAvailable { get; set; }
        }
   
}