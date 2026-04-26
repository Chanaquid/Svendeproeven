using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/appeals")]
    [ApiController]
    [Authorize]
    public class AppealController : BaseController
    {
        private readonly IAppealService _appealService;

        public AppealController(IAppealService appealService)
        {
            _appealService = appealService;
        }

        // GET /api/appeals/my
        [HttpGet("my")]
        public async Task<ActionResult<ApiResponse<PagedResult<AppealDto>>>> GetMyAppeals(
            [FromQuery] AppealFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _appealService.GetMyAppealsAsync(Caller.UserId, filter, request);
            return Ok(ApiResponse<PagedResult<AppealDto>>.Ok(result));
        }

        // GET /api/appeals/{id}  — regular users see AppealDto, admins are redirected to /admin/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<AppealDto>>> GetById(int id)
        {
            var result = await _appealService.GetByIdAsync(id, Caller.UserId, isAdmin: false);
            return Ok(ApiResponse<AppealDto>.Ok(result));
        }

        // POST /api/appeals/score
        [HttpPost("score")]
        public async Task<ActionResult<ApiResponse<AppealDto>>> CreateScoreAppeal(
            [FromBody] CreateScoreAppealDto dto)
        {
            var result = await _appealService.CreateScoreAppealAsync(Caller.UserId, dto);
            return Ok(ApiResponse<AppealDto>.Ok(result, "Score appeal submitted successfully."));
        }

        // POST /api/appeals/fine
        [HttpPost("fine")]
        public async Task<ActionResult<ApiResponse<AppealDto>>> CreateFineAppeal(
            [FromBody] CreateFineAppealDto dto)
        {
            var result = await _appealService.CreateFineAppealAsync(Caller.UserId, dto);
            return Ok(ApiResponse<AppealDto>.Ok(result, "Fine appeal submitted successfully."));
        }

        // PATCH /api/appeals/{id}/cancel
        [HttpPatch("{id}/cancel")]
        public async Task<ActionResult<ApiResponse<string>>> CancelAppeal(int id)
        {
            await _appealService.CancelAppealAsync(id, Caller.UserId);
            return Ok(ApiResponse<string>.Ok(null, "Appeal cancelled successfully."));
        }

        // DELETE /api/appeals/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteAppeal(int id)
        {
            await _appealService.DeleteAppealAsync(id, Caller.UserId);
            return Ok(ApiResponse<string>.Ok(null, "Appeal deleted successfully."));
        }


        //Admin Endpoints

        // GET /api/appeals
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedResult<AppealDto>>>> GetAllAppeals(
            [FromQuery] AppealFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _appealService.GetAllAppealsAsync(filter, request);
            return Ok(ApiResponse<PagedResult<AppealDto>>.Ok(result));
        }

        // GET /api/appeals/pending
        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedResult<AppealDto>>>> GetAllPending(
            [FromQuery] AppealFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _appealService.GetAllPendingAsync(filter, request);
            return Ok(ApiResponse<PagedResult<AppealDto>>.Ok(result));
        }

        // GET /api/appeals/admin/{id}  — returns full AdminAppealDto with user stats
        [HttpGet("admin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<AdminAppealDto>>> GetByIdForAdmin(int id)
        {
            var result = await _appealService.GetByIdWithDetailsAsync(id);
            return Ok(ApiResponse<AdminAppealDto>.Ok((AdminAppealDto)result));
        }

        // GET /api/appeals/user/{userId}
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedResult<AppealDto>>>> GetAllByUserId(
            string userId,
            [FromQuery] AppealFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _appealService.GetAllAppealsByUserIdAsync(userId, filter, request);
            return Ok(ApiResponse<PagedResult<AppealDto>>.Ok(result));
        }

        // GET /api/appeals/status/{status}
        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedResult<AppealDto>>>> GetAllByStatus(
            AppealStatus status,
            [FromQuery] AppealFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _appealService.GetAllByStatusAsync(status, filter, request);
            return Ok(ApiResponse<PagedResult<AppealDto>>.Ok(result));
        }

        // POST /api/appeals/{id}/decide/score
        [HttpPost("{id}/decide/score")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<AdminAppealDto>>> DecideScoreAppeal(
            int id,
            [FromBody] AdminDecidesScoreAppealDto dto)
        {
            var result = await _appealService.DecideScoreAppealAsync(id, Caller.UserId, dto);
            return Ok(ApiResponse<AdminAppealDto>.Ok((AdminAppealDto)result, "Score appeal decision recorded."));
        }

        // POST /api/appeals/{id}/decide/fine
        [HttpPost("{id}/decide/fine")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<AdminAppealDto>>> DecideFineAppeal(
            int id,
            [FromBody] AdminDecidesFineAppealDto dto)
        {
            var result = await _appealService.DecideFineAppealAsync(id, Caller.UserId, dto);
            return Ok(ApiResponse<AdminAppealDto>.Ok((AdminAppealDto)result, "Fine appeal decision recorded."));
        }
    }
}