using backend.Common;
using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/disputes")]
    [Authorize]
    public class DisputeController : BaseController
    {
        private readonly IDisputeService _disputeService;

        public DisputeController(IDisputeService disputeService)
        {
            _disputeService = disputeService;
        }

        // ── Filing ────────────────────────────────────────────────────

        // POST: api/disputes
        [HttpPost]
        public async Task<ActionResult<ApiResponse<DisputeDto>>> CreateDispute(
            [FromBody] CreateDisputeDto dto)
        {
            var result = await _disputeService.CreateDisputeAsync(Caller.UserId, dto);
            return CreatedAtAction(nameof(GetDisputeById), new { id = result.Id },
                ApiResponse<DisputeDto>.Ok(result, "Dispute filed successfully."));
        }

        // PUT: api/disputes/{id}
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<DisputeDto>>> EditDispute(
            int id,
            [FromBody] EditDisputeDto dto)
        {
            var result = await _disputeService.EditDisputeAsync(Caller.UserId, id, dto);
            return Ok(ApiResponse<DisputeDto>.Ok(result, "Dispute updated successfully."));
        }

        // DELETE: api/disputes/{id}
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<string>>> CancelDispute(int id)
        {
            await _disputeService.CancelDisputeAsync(Caller.UserId, id);
            return Ok(ApiResponse<string>.Ok(null, "Dispute cancelled."));
        }

        // ── Evidence — filer ──────────────────────────────────────────

        // POST: api/disputes/{id}/photos/filed
        [HttpPost("{id:int}/photos/filed")]
        public async Task<ActionResult<ApiResponse<DisputePhotoDto>>> AddFiledByPhoto(
            int id,
            [FromBody] AddDisputePhotoDto dto)
        {
            var result = await _disputeService.AddFiledByPhotoUrlAsync(Caller.UserId, id, dto.PhotoUrl);
            return Ok(ApiResponse<DisputePhotoDto>.Ok(result, "Photo added successfully."));
        }

        // DELETE: api/disputes/{id}/photos/filed/{photoId}
        [HttpDelete("{id:int}/photos/filed/{photoId:int}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteFiledByPhoto(int id, int photoId)
        {
            await _disputeService.DeleteFiledByPhotoAsync(Caller.UserId, id, photoId);
            return Ok(ApiResponse<string>.Ok(null, "Photo deleted."));
        }

        // ── Other party ───────────────────────────────────────────────

        // POST: api/disputes/{id}/viewed
        [HttpPost("{id:int}/viewed")]
        public async Task<ActionResult<ApiResponse<string>>> MarkViewed(int id)
        {
            await _disputeService.MarkViewedByOtherPartyAsync(Caller.UserId, id);
            return Ok(ApiResponse<string>.Ok(null, "Marked as viewed."));
        }

        // POST: api/disputes/{id}/response
        [HttpPost("{id:int}/response")]
        public async Task<ActionResult<ApiResponse<DisputeDto>>> SubmitResponse(
            int id,
            [FromBody] SubmitDisputeResponseDto dto)
        {
            var result = await _disputeService.SubmitResponseAsync(Caller.UserId, id, dto);
            return Ok(ApiResponse<DisputeDto>.Ok(result, "Response submitted successfully."));
        }

        // POST: api/disputes/{id}/photos/response
        [HttpPost("{id:int}/photos/response")]
        public async Task<ActionResult<ApiResponse<DisputePhotoDto>>> AddResponsePhoto(
            int id,
            [FromBody] AddDisputePhotoDto dto)
        {
            var result = await _disputeService.AddResponsePhotoUrlAsync(Caller.UserId, id, dto.PhotoUrl);
            return Ok(ApiResponse<DisputePhotoDto>.Ok(result, "Photo added successfully."));
        }

        // ── User queries ──────────────────────────────────────────────

        // GET: api/disputes/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<DisputeDto>>> GetDisputeById(int id)
        {
            var result = await _disputeService.GetDisputeByIdAsync(id, Caller.UserId);
            return Ok(ApiResponse<DisputeDto>.Ok(result));
        }

        // GET: api/disputes/my/filed
        [HttpGet("my/filed")]
        public async Task<ActionResult<ApiResponse<PagedResult<DisputeListDto>>>> GetMyFiledDisputes(
            [FromQuery] DisputeFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _disputeService.GetFiledByUserAsync(Caller.UserId, filter, request);
            return Ok(ApiResponse<PagedResult<DisputeListDto>>.Ok(result));
        }

        // GET: api/disputes/my/responding
        [HttpGet("my/responding")]
        public async Task<ActionResult<ApiResponse<PagedResult<DisputeListDto>>>> GetMyRespondingDisputes(
            [FromQuery] DisputeFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _disputeService.GetRespondedToByUserAsync(Caller.UserId, filter, request);
            return Ok(ApiResponse<PagedResult<DisputeListDto>>.Ok(result));
        }

        // GET: api/disputes/my/all
        [HttpGet("my/all")]
        public async Task<ActionResult<ApiResponse<PagedResult<DisputeListDto>>>> GetAllMyDisputes(
            [FromQuery] DisputeFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _disputeService.GetAllDisputesByUserIdAsync(Caller.UserId, filter, request);
            return Ok(ApiResponse<PagedResult<DisputeListDto>>.Ok(result));
        }

        // GET: api/disputes/can-file/{loanId}
        [HttpGet("can-file/{loanId:int}")]
        public async Task<ActionResult<ApiResponse<object>>> CanFileDispute(int loanId)
        {
            var canFile = await _disputeService.CanUserFileDisputeAsync(Caller.UserId, loanId);
            return Ok(ApiResponse<object>.Ok(new { canFile }));
        }

        // ── Admin ─────────────────────────────────────────────────────

        // GET: api/disputes/admin/{id}
        [HttpGet("admin/{id:int}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ApiResponse<DisputeDto>>> AdminGetDisputeById(int id)
        {
            var result = await _disputeService.AdminGetDisputeByIdAsync(id);
            return Ok(ApiResponse<DisputeDto>.Ok(result));
        }

        // GET: api/disputes/admin/all
        [HttpGet("admin/all")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ApiResponse<PagedResult<DisputeListDto>>>> GetAllDisputes(
            [FromQuery] DisputeFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _disputeService.GetAllDisputesAsync(filter, request);
            return Ok(ApiResponse<PagedResult<DisputeListDto>>.Ok(result));
        }

        // GET: api/disputes/admin/open
        [HttpGet("admin/open")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ApiResponse<PagedResult<DisputeListDto>>>> GetAllOpenDisputes(
            [FromQuery] DisputeFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _disputeService.GetAllOpenDisputesAsync(filter, request);
            return Ok(ApiResponse<PagedResult<DisputeListDto>>.Ok(result));
        }

        // GET: api/disputes/admin/status/{status}
        [HttpGet("admin/status/{status}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ApiResponse<PagedResult<DisputeListDto>>>> GetDisputesByStatus(
            DisputeStatus status,
            [FromQuery] DisputeFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _disputeService.GetDisputesByStatusAsync(status, filter, request);
            return Ok(ApiResponse<PagedResult<DisputeListDto>>.Ok(result));
        }

        // GET: api/disputes/admin/loan/{loanId}
        [HttpGet("admin/loan/{loanId:int}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ApiResponse<List<DisputeDto>>>> GetDisputesByLoan(int loanId)
        {
            var result = await _disputeService.GetDisputesByLoanIdAsync(loanId);
            return Ok(ApiResponse<List<DisputeDto>>.Ok(result));
        }

        // GET: api/disputes/admin/item/{itemId}/history
        [HttpGet("admin/item/{itemId:int}/history")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ApiResponse<PagedResult<DisputeListDto>>>> GetDisputeHistoryByItem(
            int itemId,
            [FromQuery] DisputeFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _disputeService.GetDisputeHistoryByItemAsync(itemId, filter, request);
            return Ok(ApiResponse<PagedResult<DisputeListDto>>.Ok(result));
        }

        // POST: api/disputes/admin/{id}/resolve
        [HttpPost("admin/{id:int}/resolve")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ApiResponse<DisputeDto>>> ResolveDispute(
            int id,
            [FromBody] AdminResolveDisputeDto dto)
        {
            var result = await _disputeService.ResolveDisputeAsync(Caller.UserId, id, dto);
            return Ok(ApiResponse<DisputeDto>.Ok(result, "Dispute resolved successfully."));
        }

        // GET: api/disputes/admin/stats
        [HttpGet("admin/stats")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ApiResponse<DisputeStatsDto>>> GetDisputeStats()
        {
            var result = await _disputeService.GetDisputeStatsAsync();
            return Ok(ApiResponse<DisputeStatsDto>.Ok(result));
        }
    }
}