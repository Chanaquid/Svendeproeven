using backend.Dtos;
using backend.Models;

namespace backend.Interfaces
{
    public interface IDirectMessageRepository
    {
        Task AddAsync(DirectMessage message);

        Task<PagedResult<DirectMessage>> GetConversationMessagesAsync(int conversationId,
            string userId, MessageFilter? filter, PagedRequest request);

        //Unread count for a specific conversation
        Task<int> GetUnreadCountForConversationAsync(int conversationId, string userId, DateTime? afterDate = null);

        //Total unread messages across all conversations for a user
        Task<int> GetTotalUnreadCountForUserAsync(string userId);

        //Latest message in a conversation
        Task<DirectMessage?> GetLastMessageAsync(int conversationId);

        Task<DirectMessage?> GetByIdAsync(int messageId);

        void Update(DirectMessage message);

        Task SaveChangesAsync();

        //Mark messages as read (optionally up to a specific message)
        Task MarkMessagesAsReadAsync(int conversationId, string userId, int? upToMessageId = null);

        Task<int> GetMessageCountAsync(int conversationId);


    }
}