using backend.Dtos;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/ban-history")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UserBanHistoryController : BaseController
    {
        private readonly IUserBanHistoryService _banHistoryService;

        public UserBanHistoryController(IUserBanHistoryService banHistoryService)
        {
            _banHistoryService = banHistoryService;
        }

        // GET /api/ban-history
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<UserBanHistoryDto>>>> GetAll(
            [FromQuery] UserBanHistoryFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _banHistoryService.GetAllAsync(filter, request);
            return Ok(ApiResponse<PagedResult<UserBanHistoryDto>>.Ok(result));
        }

        // GET /api/ban-history/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<UserBanHistoryDto>>> GetById(int id)
        {
            var result = await _banHistoryService.GetByIdAsync(id);
            return Ok(ApiResponse<UserBanHistoryDto>.Ok(result));
        }

        // GET /api/ban-history/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<ApiResponse<PagedResult<UserBanHistoryDto>>>> GetByUserId(
            string userId,
            [FromQuery] UserBanHistoryFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _banHistoryService.GetByUserIdAsync(userId, filter, request);
            return Ok(ApiResponse<PagedResult<UserBanHistoryDto>>.Ok(result));
        }
    }
}