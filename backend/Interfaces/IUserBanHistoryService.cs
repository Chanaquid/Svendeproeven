using backend.Dtos;

namespace backend.Interfaces
{
    public interface IUserBanHistoryService
    {
        Task<PagedResult<UserBanHistoryDto>> GetByUserIdAsync(string userId, UserBanHistoryFilter? filter, PagedRequest request);
        Task<PagedResult<UserBanHistoryDto>> GetAllAsync(UserBanHistoryFilter? filter, PagedRequest request);
        Task<UserBanHistoryDto> GetByIdAsync(int id);
    }
}