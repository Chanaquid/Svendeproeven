using backend.Dtos;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/score-history")]
    [ApiController]
    [Authorize]
    public class ScoreHistoryController : BaseController
    {
        private readonly IScoreHistoryService _scoreHistoryService;

        public ScoreHistoryController(IScoreHistoryService scoreHistoryService)
        {
            _scoreHistoryService = scoreHistoryService;
        }

        // GET /api/score-history/my
        [HttpGet("my")]
        public async Task<ActionResult<ApiResponse<PagedResult<ScoreHistoryDto>>>> GetMyHistory(
            [FromQuery] ScoreHistoryFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _scoreHistoryService.GetMyHistoryAsync(Caller.UserId, filter, request);
            return Ok(ApiResponse<PagedResult<ScoreHistoryDto>>.Ok(result));
        }

        // GET /api/score-history/my/summary
        [HttpGet("my/summary")]
        public async Task<ActionResult<ApiResponse<UserScoreSummaryDto>>> GetMyScoreSummary()
        {
            var result = await _scoreHistoryService.GetMyScoreSummaryAsync(Caller.UserId);
            return Ok(ApiResponse<UserScoreSummaryDto>.Ok(result));
        }

        // -------------------- Admin Endpoints --------------------

        // GET /api/score-history
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedResult<ScoreHistoryDto>>>> GetAll(
            [FromQuery] ScoreHistoryFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _scoreHistoryService.GetAllAsync(filter, request);
            return Ok(ApiResponse<PagedResult<ScoreHistoryDto>>.Ok(result));
        }

        // GET /api/score-history/user/{userId}
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedResult<ScoreHistoryDto>>>> GetByUserId(
            string userId,
            [FromQuery] ScoreHistoryFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _scoreHistoryService.GetByUserIdAsync(userId, filter, request);
            return Ok(ApiResponse<PagedResult<ScoreHistoryDto>>.Ok(result));
        }

        // GET /api/score-history/user/{userId}/summary
        [HttpGet("user/{userId}/summary")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<UserScoreSummaryDto>>> GetScoreSummaryByUserId(string userId)
        {
            var result = await _scoreHistoryService.GetScoreSummaryByUserIdAsync(userId);
            return Ok(ApiResponse<UserScoreSummaryDto>.Ok(result));
        }

        // POST /api/score-history/adjust
        [HttpPost("adjust")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<string>>> AdminAdjustScore(
            [FromBody] AdminAdjustScoreDto dto)
        {
            await _scoreHistoryService.AdminAdjustScoreAsync(Caller.UserId, dto);
            return Ok(ApiResponse<string>.Ok(null, "Score adjusted successfully."));
        }
    }
}