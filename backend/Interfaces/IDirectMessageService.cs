using backend.Dtos;
using backend.Models;

namespace backend.Interfaces
{
    public interface IDirectMessageService
    {
        Task<DirectMessageDto> SendMessageAsync(int conversationId, string senderId, string content);

        Task<PagedResult<DirectMessageDto>> GetConversationMessagesAsync(int conversationId,
            string userId, MessageFilter? filter, PagedRequest request);

        Task MarkMessagesAsReadAsync(int conversationId, string userId);

        Task<int> GetUnreadCountAsync(string userId);

        Task<DirectMessageDto?> GetLastMessageAsync(int conversationId, string currentUserId);


    }
}