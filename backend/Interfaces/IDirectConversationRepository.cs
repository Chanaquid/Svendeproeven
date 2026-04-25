using backend.Dtos;
using backend.Models;
using System.Runtime.CompilerServices;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace backend.Interfaces
{
    public interface IDirectConversationRepository
    {
        Task<DirectConversation?> GetByIdAsync(int conversationId);
        Task<DirectConversation?> GetConversationBetweenUsersAsync(string userId1, string userId2);
        Task<DirectConversation> CreateAsync(string initiatedById, string otherUserId);

        Task<PagedResult<DirectConversation>> GetUserConversationsAsync(string userId,
            ConversationFilter? filter,
            PagedRequest request);

        Task<HashSet<string>> GetBlockedUserIdsAsync(string userId);
        Task<Dictionary<int, int>> GetUnreadCountsForUserAsync(string userId);

        Task<bool> AreUsersBlockedAsync(string userId1, string userId2);
        Task<HashSet<string>> GetOutgoingBlockedUserIdsAsync(string userId); //for dm 
        Task<bool> IsBlockedByCurrentUserAsync(string blockerId, string blockedId);
        void Update(DirectConversation conversation);
        Task SaveChangesAsync();
    }
}
