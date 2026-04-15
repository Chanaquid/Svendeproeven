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


}
