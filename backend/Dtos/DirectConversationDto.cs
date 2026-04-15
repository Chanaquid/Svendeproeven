using System.ComponentModel.DataAnnotations;

namespace backend.Dtos
{
    public class CreateDirectConversationDto
    {

        [Required(ErrorMessage = "Initial message is required")]
        [MinLength(1, ErrorMessage = "Message cannot be empty")]
        [MaxLength(2000, ErrorMessage = "Message cannot exceed 2000 characters")]
        public string InitialMessage { get; set; } = string.Empty;
    }

    //listings sidebar or showing many convos
    public class DirectConversationListDto
    {
        public int Id { get; set; }

        public string OtherUserId { get; set; } = string.Empty;
        public string OtherUserFullName{ get; set; } = string.Empty;
        public string OtherUserName{ get; set; } = string.Empty;
        public string? OtherUserAvatarUrl { get; set; }

        //Latest message info for preview
        public string? LastMessageContent { get; set; }
        public DateTime? LastMessageSentAt { get; set; }
        public string? LastMessageSenderId { get; set; }
        public string? LastMessageSenderName { get; set; } //Optional?
        public string? LastMessageAvatarUrl { get; set; }//Optional?

        public bool IsBlocked { get; set; }
        public int UnreadCount { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsInitiatedByMe { get; set; }

        public bool IsUnread => UnreadCount > 0;


    }

    //when clicking on one convo, expands all
    public class DirectConversationDto
    {
        public int Id { get; set; }

        public string InitiatedById { get; set; } = string.Empty;
        public string InitiatedByFullName { get; set; } = string.Empty;
        public string InitiatedByUserName { get; set; } = string.Empty;        
        public string? InitiatedByAvatarUrl { get; set; }

        public string OtherUserId { get; set; } = string.Empty;
        public string OtherUserFullName { get; set; } = string.Empty;
        public string OtherUserName { get; set; } = string.Empty;
        public string? OtherUserAvatarUrl { get; set; }

        public bool HiddenForInitiator { get; set; }
        public bool HiddenForOther { get; set; }

        public bool IsBlocked { get; set; }

        public DateTime? InitiatorDeletedAt { get; set; }
        public DateTime? OtherDeletedAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }

        public bool IsHiddenForCurrentUser { get; set; }
        public DateTime? DeletedAtForCurrentUser { get; set; }
        public bool CanSendMessage { get; set; }

        public int UnreadCount { get; set; }

        public List<DirectMessageDto> Messages { get; set; } = new();

        public string OtherUserDisplayName => !string.IsNullOrWhiteSpace(OtherUserName)
            ? OtherUserName
            : "Unknown User";
    }

    public class UnreadCountsDto
    {
        public Dictionary<int, int> ConversationUnreadCounts { get; set; } = new();
        public int TotalUnreadCount { get; set; }
    }
    

}




