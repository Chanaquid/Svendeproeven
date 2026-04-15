using backend.Dtos;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/verification")]
    [ApiController]
    [Authorize]
    public class VerificationRequestController : BaseController
    {
        private readonly IVerificationRequestService _verificationService;

        public VerificationRequestController(IVerificationRequestService verificationService)
        {
            _verificationService = verificationService;
        }

        // POST /api/verification
        [HttpPost]
        public async Task<ActionResult<ApiResponse<VerificationRequestDto>>> SubmitRequest(
            [FromBody] CreateVerificationRequestDto dto)
        {
            var result = await _verificationService.SubmitRequestAsync(Caller.UserId, dto);
            return Ok(ApiResponse<VerificationRequestDto>.Ok(result, "Verification request submitted successfully."));
        }

        // GET /api/verification/my
        // Returns full history — all past rejected + current pending/approved
        [HttpGet("my")]
        public async Task<ActionResult<ApiResponse<PagedResult<VerificationRequestDto>>>> GetMyRequests(
            [FromQuery] VerificationRequestFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _verificationService.GetMyRequestsAsync(Caller.UserId, filter, request);
            return Ok(ApiResponse<PagedResult<VerificationRequestDto>>.Ok(result));
        }

        // GET /api/verification/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<VerificationRequestDto>>> GetById(int id)
        {
            var result = await _verificationService.GetByIdAsync(id, Caller.UserId, Caller.IsAdmin);
            return Ok(ApiResponse<VerificationRequestDto>.Ok(result));
        }

        // -------------------- Admin Endpoints --------------------

        // GET /api/verification
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedResult<VerificationRequestDto>>>> GetAll(
            [FromQuery] VerificationRequestFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _verificationService.GetAllAsync(filter, request);
            return Ok(ApiResponse<PagedResult<VerificationRequestDto>>.Ok(result));
        }

        // GET /api/verification/user/{userId}
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedResult<VerificationRequestDto>>>> GetByUserId(
            string userId,
            [FromQuery] VerificationRequestFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _verificationService.GetByUserIdAsync(userId, filter, request);
            return Ok(ApiResponse<PagedResult<VerificationRequestDto>>.Ok(result));
        }

        // POST /api/verification/{id}/decide
        [HttpPost("{id}/decide")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<VerificationRequestDto>>> Decide(
            int id,
            [FromBody] AdminDecideVerificationRequestDto dto)
        {
            var result = await _verificationService.DecideAsync(id, Caller.UserId, dto);
            return Ok(ApiResponse<VerificationRequestDto>.Ok(result, "Verification decision recorded."));
        }
    }
}