using backend.Dtos;
using backend.Interfaces;
using backend.Models;

namespace backend.Services
{
    public class ScoreHistoryService : IScoreHistoryService
    {
        private readonly IScoreHistoryRepository _scoreHistoryRepository;
        private readonly IUserRepository _userRepository;

        public ScoreHistoryService(
            IScoreHistoryRepository scoreHistoryRepository,
            IUserRepository userRepository)
        {
            _scoreHistoryRepository = scoreHistoryRepository;
            _userRepository = userRepository;
        }

        //User 

        public async Task<PagedResult<ScoreHistoryDto>> GetMyHistoryAsync(
            string userId,
            ScoreHistoryFilter? filter,
            PagedRequest request)
        {
            var paged = await _scoreHistoryRepository.GetByUserIdAsync(userId, filter, request);
            return MapPagedResult(paged);
        }

        public async Task<UserScoreSummaryDto> GetMyScoreSummaryAsync(string userId)
        {
            return await _scoreHistoryRepository.GetScoreSummaryByUserIdAsync(userId);
        }

        //Admin

        public async Task<PagedResult<ScoreHistoryDto>> GetByUserIdAsync(
            string userId,
            ScoreHistoryFilter? filter,
            PagedRequest request)
        {
            //Verify user exists before querying
            _ = await _userRepository.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            var paged = await _scoreHistoryRepository.GetByUserIdAsync(userId, filter, request);
            return MapPagedResult(paged);
        }

        public async Task<PagedResult<ScoreHistoryDto>> GetAllAsync(
            ScoreHistoryFilter? filter,
            PagedRequest request)
        {
            var paged = await _scoreHistoryRepository.GetAllAsync(filter, request);
            return MapPagedResult(paged);
        }

        public async Task<UserScoreSummaryDto> GetScoreSummaryByUserIdAsync(string userId)
        {
            _ = await _userRepository.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            return await _scoreHistoryRepository.GetScoreSummaryByUserIdAsync(userId);
        }

        public async Task AdminAdjustScoreAsync(string adminId, AdminAdjustScoreDto dto)
        {
            var user = await _userRepository.GetByIdAsync(dto.UserId)
                ?? throw new KeyNotFoundException("User not found.");

            var newScore = Math.Clamp(user.Score + dto.PointsChanged, 0, 100);
            var actualPointsChanged = newScore - user.Score;

            //cap scores max and min
            if (actualPointsChanged == 0 && dto.PointsChanged != 0)
                throw new InvalidOperationException(
                    dto.PointsChanged > 0
                        ? "User score is already at 100."
                        : "User score is already at 0.");

            var scoreHistory = new ScoreHistory
            {
                UserId = user.Id,
                PointsChanged = actualPointsChanged,
                ScoreAfterChange = newScore,
                Reason = dto.Reason,
                LoanId = dto.LoanId,
                Note = dto.Note?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            user.Score = newScore;
            _userRepository.Update(user);

            await _scoreHistoryRepository.AddAsync(scoreHistory);
            await _scoreHistoryRepository.SaveChangesAsync();
        }

        //Helpers

        private static ScoreHistoryDto MapToDto(ScoreHistory s)
        {
            return new ScoreHistoryDto
            {
                Id = s.Id,
                PointsChanged = s.PointsChanged,
                ScoreAfterChange = s.ScoreAfterChange,
                Reason = s.Reason,
                Note = s.Note,
                CreatedAt = s.CreatedAt
            };
        }

        private static PagedResult<ScoreHistoryDto> MapPagedResult(PagedResult<ScoreHistory> source)
        {
            return new PagedResult<ScoreHistoryDto>
            {
                Items = source.Items.Select(MapToDto).ToList(),
                TotalCount = source.TotalCount,
                Page = source.Page,
                PageSize = source.PageSize
            };
        }
    }
}