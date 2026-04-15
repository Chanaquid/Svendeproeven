using backend.Dtos;
using backend.Models;

namespace backend.Interfaces
{
    //Combined SupportThread and SupportMessage in one domain "SupportChat"
    public interface ISupportRepository
    {
        //Thread
        Task<SupportThread?> GetThreadByIdAsync(int id);
        Task<SupportThread?> GetThreadByIdWithMessagesAsync(int id);
        Task<SupportThread?> GetActiveThreadByUserIdAsync(string userId);
        Task<PagedResult<SupportThread>> GetThreadsByUserIdAsync(string userId, SupportThreadFilter? filter, PagedRequest request);
        Task<PagedResult<SupportThread>> GetAllThreadsAsync(SupportThreadFilter? filter, PagedRequest request);
        Task<List<SupportThread>> GetInactiveOpenThreadsAsync(DateTime inactiveSince);
        Task<bool> HasActiveThreadAsync(string userId);
        Task AddThreadAsync(SupportThread thread);
        void UpdateThread(SupportThread thread);

        //Message
        Task<SupportMessage?> GetMessageByIdAsync(int id);
        Task<PagedResult<SupportMessage>> GetMessagesAsync(int threadId, PagedRequest request);
        Task AddMessageAsync(SupportMessage message);
        Task MarkMessagesAsReadAsync(int threadId, string userId, int? upToMessageId = null);
        Task<int> GetUnreadCountAsync(int threadId, string userId);
        Task<SupportMessage?> GetLastMessageAsync(int threadId);

        Task SaveChangesAsync();
    }
}