using backend.Dtos;

namespace backend.Interfaces
{
    public interface IScoreHistoryService
    {
        //User
        Task<PagedResult<ScoreHistoryDto>> GetMyHistoryAsync(string userId, ScoreHistoryFilter? filter, PagedRequest request);
        Task<UserScoreSummaryDto> GetMyScoreSummaryAsync(string userId);

        //Admin
        Task<PagedResult<ScoreHistoryDto>> GetByUserIdAsync(string userId, ScoreHistoryFilter? filter, PagedRequest request);
        Task<PagedResult<ScoreHistoryDto>> GetAllAsync(ScoreHistoryFilter? filter, PagedRequest request);
        Task<UserScoreSummaryDto> GetScoreSummaryByUserIdAsync(string userId);
        Task AdminAdjustScoreAsync(string adminId, AdminAdjustScoreDto dto);
    }
}