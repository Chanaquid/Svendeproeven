using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/fines")]
    [Authorize]
    public class FineController : BaseController
    {
        private readonly IFineService _fineService;

        public FineController(IFineService fineService)
        {
            _fineService = fineService;
        }

        //User endpoints

        [HttpGet("my")]
        public async Task<ActionResult<ApiResponse<PagedResult<FineListDto>>>> GetMyFines(
            [FromQuery] FineFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _fineService.GetMyFinesAsync(Caller.UserId, filter, request);
            return Ok(ApiResponse<PagedResult<FineListDto>>.Ok(result));
        }

        [HttpGet("my/{fineId:int}")]
        public async Task<ActionResult<ApiResponse<FineDto>>> GetMyFineById(int fineId)
        {
            var fine = await _fineService.GetFineByIdAsync(fineId, Caller.UserId);
            return Ok(ApiResponse<FineDto>.Ok(fine));
        }

        [HttpPost("my/submit-proof")]
        public async Task<ActionResult<ApiResponse<FineDto>>> SubmitPaymentProof([FromBody] SubmitPaymentProofDto dto)
        {
            var fine = await _fineService.SubmitPaymentProofAsync(Caller.UserId, dto);
            return Ok(ApiResponse<FineDto>.Ok(fine, "Payment proof submitted successfully."));
        }

        //Admin endpoints

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedResult<FineListDto>>>> GetAllFines(
            [FromQuery] FineFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _fineService.GetAllFinesAsync(filter, request);
            return Ok(ApiResponse<PagedResult<FineListDto>>.Ok(result));
        }

        [HttpGet("{fineId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<FineDto>>> GetFineById(int fineId)
        {
            var fine = await _fineService.AdminGetFineByIdAsync(fineId);
            return Ok(ApiResponse<FineDto>.Ok(fine));
        }

        [HttpGet("by-status/{status}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedResult<FineListDto>>>> GetFinesByStatus(
            FineStatus status,
            [FromQuery] FineFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _fineService.GetFinesByStatusAsync(status, filter, request);
            return Ok(ApiResponse<PagedResult<FineListDto>>.Ok(result));
        }

        [HttpGet("pending-proof-review")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedResult<FineListDto>>>> GetPendingProofReview(
            [FromQuery] FineFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _fineService.GetPendingProofReviewAsync(filter, request);
            return Ok(ApiResponse<PagedResult<FineListDto>>.Ok(result));
        }

        [HttpGet("by-loan/{loanId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<List<FineDto>>>> GetFinesByLoan(int loanId)
        {
            var fines = await _fineService.GetFinesByLoanIdAsync(loanId);
            return Ok(ApiResponse<List<FineDto>>.Ok(fines));
        }

        [HttpGet("by-dispute/{disputeId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<List<FineDto>>>> GetFinesByDispute(int disputeId)
        {
            var fines = await _fineService.GetFinesByDisputeIdAsync(disputeId);
            return Ok(ApiResponse<List<FineDto>>.Ok(fines));
        }

        [HttpGet("stats")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<FineStatsDto>>> GetStats()
        {
            var stats = await _fineService.GetFineStatsAsync();
            return Ok(ApiResponse<FineStatsDto>.Ok(stats));
        }

        [HttpPost("issue/loan-dispute")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<FineDto>>> CreateLoanDisputeFine([FromBody] CreateLoanDisputeFineDto dto)
        {
            var fine = await _fineService.CreateLoanDisputeFineAsync(Caller.UserId, dto);
            return CreatedAtAction(nameof(GetFineById), new { fineId = fine.Id },
                ApiResponse<FineDto>.Ok(fine, "Fine issued successfully."));
        }

        [HttpPost("issue/custom")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<FineDto>>> CreateCustomFine([FromBody] CreateCustomFineDto dto)
        {
            var fine = await _fineService.CreateCustomFineAsync(Caller.UserId, dto);
            return CreatedAtAction(nameof(GetFineById), new { fineId = fine.Id },
                ApiResponse<FineDto>.Ok(fine, "Fine issued successfully."));
        }

        [HttpPut("{fineId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<FineDto>>> UpdateFine(int fineId, [FromBody] UpdateFineDto dto)
        {
            if (fineId != dto.FineId)
                return BadRequest(ApiResponse<FineDto>.Fail("Route fineId does not match body FineId"));

            var fine = await _fineService.UpdateFineAsync(Caller.UserId, dto);
            return Ok(ApiResponse<FineDto>.Ok(fine, "Fine updated successfully."));
        }

        [HttpPut("{fineId:int}/void")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<string>>> VoidFine(int fineId)
        {
            await _fineService.VoidFineAsync(Caller.UserId, fineId);
            return Ok(ApiResponse<string>.Ok(null, "Fine voided successfully."));
        }

        [HttpPut("{fineId:int}/verify-payment")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<FineDto>>> VerifyPayment(int fineId, [FromBody] AdminFineVerifyPaymentDto dto)
        {
            var fine = await _fineService.VerifyPaymentAsync(Caller.UserId,fineId, dto);
            var message = dto.IsApproved ? "Payment approved successfully." : "Payment proof rejected.";
            return Ok(ApiResponse<FineDto>.Ok(fine, message));
        }
    }
}