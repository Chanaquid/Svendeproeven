using backend.Dtos;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/support")]
    [ApiController]
    [Authorize]
    public class SupportController : BaseController
    {
        private readonly ISupportService _supportService;

        public SupportController(ISupportService supportService)
        {
            _supportService = supportService;
        }

        // POST /api/support
        [HttpPost]
        public async Task<ActionResult<ApiResponse<SupportThreadDto>>> CreateThread(
            [FromBody] CreateSupportThreadDto dto)
        {
            var result = await _supportService.CreateThreadAsync(Caller.UserId, dto);
            return Ok(ApiResponse<SupportThreadDto>.Ok(result, "Support thread created successfully."));
        }

        // GET /api/support/my
        [HttpGet("my")]
        public async Task<ActionResult<ApiResponse<PagedResult<SupportThreadListDto>>>> GetMyThreads(
            [FromQuery] SupportThreadFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _supportService.GetMyThreadsAsync(Caller.UserId, filter, request);
            return Ok(ApiResponse<PagedResult<SupportThreadListDto>>.Ok(result));
        }

        // GET /api/support/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<SupportThreadDto>>> GetById(int id)
        {
            var result = await _supportService.GetThreadByIdAsync(id, Caller.UserId, Caller.IsAdmin);
            return Ok(ApiResponse<SupportThreadDto>.Ok(result));
        }

        // POST /api/support/{id}/messages
        [HttpPost("{id}/messages")]
        public async Task<ActionResult<ApiResponse<SupportMessageDto>>> SendMessage(
            int id,
            [FromBody] SendSupportMessageDto dto)
        {
            var result = await _supportService.SendMessageAsync(id, Caller.UserId, dto, Caller.IsAdmin);
            return Ok(ApiResponse<SupportMessageDto>.Ok(result));
        }

        // GET /api/support/{id}/messages
        [HttpGet("{id}/messages")]
        public async Task<ActionResult<ApiResponse<PagedResult<SupportMessageDto>>>> GetMessages(
            int id,
            [FromQuery] PagedRequest request)
        {
            var result = await _supportService.GetMessagesAsync(id, Caller.UserId, Caller.IsAdmin, request);
            return Ok(ApiResponse<PagedResult<SupportMessageDto>>.Ok(result));
        }

        // PATCH /api/support/{id}/close
        [HttpPatch("{id}/close")]
        public async Task<ActionResult<ApiResponse<string>>> CloseThread(int id)
        {
            await _supportService.CloseThreadAsync(id, Caller.UserId, Caller.IsAdmin);
            return Ok(ApiResponse<string>.Ok(null, "Support thread closed."));
        }

        // PATCH /api/support/{id}/read
        [HttpPatch("{id}/read")]
        public async Task<ActionResult<ApiResponse<string>>> MarkAsRead(
            int id,
            [FromBody] MarkSupportMessagesReadDto dto)
        {
            await _supportService.MarkMessagesAsReadAsync(id, Caller.UserId, dto);
            return Ok(ApiResponse<string>.Ok(null));
        }

        //Admin endpoints

        // GET /api/support
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedResult<SupportThreadListDto>>>> GetAllThreads(
            [FromQuery] SupportThreadFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _supportService.GetAllThreadsAsync(filter, request);
            return Ok(ApiResponse<PagedResult<SupportThreadListDto>>.Ok(result));
        }

        // POST /api/support/admin/user/{userId}
        //Admin opens a thread on behalf of OR directed at a specific user
        [HttpPost("admin/user/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<SupportThreadDto>>> AdminCreateThread(
            string userId,
            [FromBody] CreateSupportThreadDto dto)
        {
            var result = await _supportService.AdminCreateThreadAsync(Caller.UserId, userId, dto);
            return Ok(ApiResponse<SupportThreadDto>.Ok(result, "Support thread created successfully."));
        }

        // POST /api/support/{id}/claim
        [HttpPost("{id}/claim")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<SupportThreadDto>>> ClaimThread(int id)
        {
            var result = await _supportService.ClaimThreadAsync(id, Caller.UserId);
            return Ok(ApiResponse<SupportThreadDto>.Ok(result, "Thread claimed successfully."));
        }
    }
}