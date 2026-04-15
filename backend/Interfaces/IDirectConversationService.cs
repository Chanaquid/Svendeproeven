using backend.Dtos;

namespace backend.Interfaces
{
    public interface IDirectConversationService
    {
        //Get existing or create new conversation - reopens same conversation if deleted
        Task<DirectConversationDto> GetOrCreateConversationAsync(string currentUserId,
             string otherUserId, string? initialMessage = null);

        Task<DirectConversationDto> GetConversationAsync(int conversationId, string currentUserId);

        Task<PagedResult<DirectConversationListDto>> GetUserConversationsAsync(string currentUserId,
            ConversationFilter? filter, PagedRequest request);

        //Soft delete (only for this user)
        Task DeleteConversationForUserAsync(int conversationId, string currentUserId);


        //Restore previously deleted conversation
        Task RestoreConversationForUserAsync(int conversationId, string currentUserId);

        //Authorization check
        Task<bool> CanUserAccessConversationAsync(int conversationId, string userId);

        //Total unread messages across all conversations
        Task<int> GetTotalUnreadCountAsync(string userId);

        //Unread count per conversation (for chat list / badges)
        Task<UnreadCountsDto> GetUnreadCountsPerConversationAsync(string userId);

    }
}
