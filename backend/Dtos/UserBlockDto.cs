using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{

    public class UserBlockDto
    {
        public string BlockerId { get; set; } = string.Empty;
        public string BlockerName { get; set; } = string.Empty;
        public string BlockerUserName { get; set; } = string.Empty;
        public string? BlockerAvatarUrl { get; set; }
        public string BlockedId { get; set; } = string.Empty;
        public string BlockedName { get; set; } = string.Empty;
        public string BlockedUserName { get; set; } = string.Empty;
        public string? BlockedAvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    
    public class UserBlockListDto
    {
        public string BlockedId { get; set; } = string.Empty;
        public string BlockedName { get; set; } = string.Empty;
        public string BlockedUserName { get; set; } = string.Empty;
        public string? BlockedAvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }





}
