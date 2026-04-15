using backend.Dtos;

namespace backend.Interfaces
{
    public interface IUserBlockService
    {
        Task BlockUserAsync(string blockerId, string blockedId);
        Task UnblockUserAsync(string blockerId, string blockedId);
        Task<bool> IsBlockedAsync(string blockerId, string blockedId);
        Task<bool> AreBlockedEitherWayAsync(string userId1, string userId2);
        Task<PagedResult<UserBlockListDto>> GetMyBlocksAsync(string userId, UserBlockFilter? filter, PagedRequest request);

        //Admin
        Task<PagedResult<UserBlockDto>> GetAllAsync(UserBlockFilter? filter, PagedRequest request);
        Task AdminUnblockAsync(string blockerId, string blockedId);
    }
}