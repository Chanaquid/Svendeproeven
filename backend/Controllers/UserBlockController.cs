using backend.Dtos;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/blocks")]
    [ApiController]
    [Authorize]
    public class UserBlockController : BaseController
    {
        private readonly IUserBlockService _blockService;

        public UserBlockController(IUserBlockService blockService)
        {
            _blockService = blockService;
        }

        // POST /api/blocks/{userId}
        [HttpPost("{userId}")]
        public async Task<ActionResult<ApiResponse<string>>> BlockUser(string userId)
        {
            await _blockService.BlockUserAsync(Caller.UserId, userId);
            return Ok(ApiResponse<string>.Ok(null, "User blocked successfully."));
        }

        // DELETE /api/blocks/{userId}
        [HttpDelete("{userId}")]
        public async Task<ActionResult<ApiResponse<string>>> UnblockUser(string userId)
        {
            await _blockService.UnblockUserAsync(Caller.UserId, userId);
            return Ok(ApiResponse<string>.Ok(null, "User unblocked successfully."));
        }

        // GET /api/blocks/my
        [HttpGet("my")]
        public async Task<ActionResult<ApiResponse<PagedResult<UserBlockListDto>>>> GetMyBlocks(
            [FromQuery] UserBlockFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _blockService.GetMyBlocksAsync(Caller.UserId, filter, request);
            return Ok(ApiResponse<PagedResult<UserBlockListDto>>.Ok(result));
        }

        // GET /api/blocks/status/{userId}
        [HttpGet("status/{userId}")]
        public async Task<ActionResult<ApiResponse<bool>>> CheckBlockStatus(string userId)
        {
            var isBlocked = await _blockService.IsBlockedAsync(Caller.UserId, userId);
            return Ok(ApiResponse<bool>.Ok(isBlocked));
        }

        //Admin Endpoints

        // GET /api/blocks
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedResult<UserBlockDto>>>> GetAll(
            [FromQuery] UserBlockFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _blockService.GetAllAsync(filter, request);
            return Ok(ApiResponse<PagedResult<UserBlockDto>>.Ok(result));
        }

        // DELETE /api/blocks/admin/{blockerId}/{blockedId}
        [HttpDelete("admin/{blockerId}/{blockedId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<string>>> AdminUnblock(string blockerId, string blockedId)
        {
            await _blockService.AdminUnblockAsync(blockerId, blockedId);
            return Ok(ApiResponse<string>.Ok(null, "Block removed successfully."));
        }
    }
}