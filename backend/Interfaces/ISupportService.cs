using backend.Dtos;

namespace backend.Interfaces
{
    public interface ISupportService
    {
        //User
        Task<SupportThreadDto> CreateThreadAsync(string userId, CreateSupportThreadDto dto);
        Task<SupportThreadDto> GetThreadByIdAsync(int id, string userId, bool isAdmin);
        Task<PagedResult<SupportThreadListDto>> GetMyThreadsAsync(string userId, SupportThreadFilter? filter, PagedRequest request);
        Task<SupportMessageDto> SendMessageAsync(int threadId, string senderId, SendSupportMessageDto dto, bool isAdmin);
        Task CloseThreadAsync(int threadId, string userId, bool isAdmin);
        Task MarkMessagesAsReadAsync(int threadId, string userId, MarkSupportMessagesReadDto dto);

        //Admin
        Task<SupportThreadDto> AdminCreateThreadAsync(string adminId, string targetUserId, CreateSupportThreadDto dto);
        Task<SupportThreadDto> ClaimThreadAsync(int threadId, string adminId);
        Task<PagedResult<SupportThreadListDto>> GetAllThreadsAsync(SupportThreadFilter? filter, PagedRequest request);

        //Background job
        Task AutoCloseInactiveThreadsAsync();
    }
}