using backend.Dtos;
using backend.Models;

namespace backend.Interfaces
{
    public interface IScoreHistoryRepository
    {
        //this is history logs. Auto generated as sideeffects(mostly)

        Task<ScoreHistory?> GetByIdAsync(int id);
        Task<List<ScoreHistory>> GetScoreHistoryByUserIdAsync(string userId);
        Task<PagedResult<ScoreHistory>> GetByUserIdAsync(string userId, ScoreHistoryFilter? filter, PagedRequest request);
        Task<PagedResult<ScoreHistory>> GetAllAsync(ScoreHistoryFilter? filter, PagedRequest request);
        Task<UserScoreSummaryDto> GetScoreSummaryByUserIdAsync(string userId);
        Task AddAsync(ScoreHistory scoreHistory);
        Task SaveChangesAsync();
    }
}