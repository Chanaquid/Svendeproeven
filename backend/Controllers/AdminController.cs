using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        //Dashboard

        // GET /api/admin/dashboard
        [HttpGet("dashboard")]
        public async Task<ActionResult<ApiResponse<AdminDashboardDto>>> GetDashboard()
        {
            var result = await _adminService.GetDashboardAsync();
            return Ok(ApiResponse<AdminDashboardDto>.Ok(result));
        }

        //Users

        // GET /api/admin/users/{userId}/ban-history
        [HttpGet("users/{userId}/ban-history")]
        public async Task<ActionResult<ApiResponse<PagedResult<UserBanHistoryDto>>>> GetBanHistory(
            string userId,
            [FromQuery] UserBanHistoryFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _adminService.GetBanHistoryAsync(userId, filter, request);
            return Ok(ApiResponse<PagedResult<UserBanHistoryDto>>.Ok(result));
        }

        // GET /api/admin/bans
        [HttpGet("bans")]
        public async Task<ActionResult<ApiResponse<PagedResult<UserBanHistoryDto>>>> GetAllBans(
            [FromQuery] UserBanHistoryFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _adminService.GetAllBansAsync(filter, request);
            return Ok(ApiResponse<PagedResult<UserBanHistoryDto>>.Ok(result));
        }

        // POST /api/admin/users/adjust-score
        [HttpPost("users/adjust-score")]
        public async Task<ActionResult<ApiResponse<string>>> AdjustUserScore(
            [FromBody] AdminAdjustScoreDto dto)
        {
            await _adminService.AdjustUserScoreAsync(Caller.UserId, dto);
            return Ok(ApiResponse<string>.Ok(null, "Score adjusted successfully."));
        }

        //Items

        // GET /api/admin/items
        [HttpGet("items")]
        public async Task<ActionResult<ApiResponse<PagedResult<ItemListDto>>>> GetAllItems(
            [FromQuery] ItemFilter filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _adminService.GetAllItemsAsync(filter, request);
            return Ok(ApiResponse<PagedResult<ItemListDto>>.Ok(result));
        }

        // GET /api/admin/items/{itemId}
        [HttpGet("items/{itemId}")]
        public async Task<ActionResult<ApiResponse<ItemDto>>> GetItemById(int itemId)
        {
            var result = await _adminService.GetItemByIdAsync(itemId);
            return Ok(ApiResponse<ItemDto>.Ok(result));
        }

        // POST /api/admin/items/{itemId}/approve
        [HttpPost("items/{itemId}/approve")]
        public async Task<ActionResult<ApiResponse<string>>> ApproveItem(int itemId)
        {
            await _adminService.ApproveItemAsync(itemId, Caller.UserId);
            return Ok(ApiResponse<string>.Ok(null, "Item approved successfully."));
        }

        // POST /api/admin/items/{itemId}/reject
        [HttpPost("items/{itemId}/reject")]
        public async Task<ActionResult<ApiResponse<string>>> RejectItem(
            int itemId,
            [FromBody] AdminRejectItemDto dto)
        {
            await _adminService.RejectItemAsync(itemId, Caller.UserId, dto.Reason);
            return Ok(ApiResponse<string>.Ok(null, "Item rejected successfully."));
        }

        // DELETE /api/admin/items/{itemId}
        [HttpDelete("items/{itemId}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteItem(int itemId)
        {
            await _adminService.DeleteItemAsync(itemId, Caller.UserId);
            return Ok(ApiResponse<string>.Ok(null, "Item deleted successfully."));
        }

        //Loans

        // GET /api/admin/loans
        [HttpGet("loans")]
        public async Task<ActionResult<ApiResponse<PagedResult<LoanListDto>>>> GetAllLoans(
            [FromQuery] LoanFilter filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _adminService.GetAllLoansAsync(filter, request);
            return Ok(ApiResponse<PagedResult<LoanListDto>>.Ok(result));
        }

        // GET /api/admin/loans/{loanId}
        [HttpGet("loans/{loanId}")]
        public async Task<ActionResult<ApiResponse<LoanDto>>> GetLoanById(int loanId)
        {
            var result = await _adminService.GetLoanByIdAsync(loanId);
            return Ok(ApiResponse<LoanDto>.Ok(result));
        }

        // POST /api/admin/loans/{loanId}/force-cancel
        [HttpPost("loans/{loanId}/force-cancel")]
        public async Task<ActionResult<ApiResponse<string>>> ForceCancelLoan(
            int loanId,
            [FromBody] AdminForceCancelLoanDto dto)
        {
            await _adminService.ForceCancelLoanAsync(loanId, Caller.UserId, dto.Reason);
            return Ok(ApiResponse<string>.Ok(null, "Loan cancelled successfully."));
        }

        //Disputes 

        // GET /api/admin/disputes
        [HttpGet("disputes")]
        public async Task<ActionResult<ApiResponse<PagedResult<DisputeListDto>>>> GetAllDisputes(
            [FromQuery] DisputeFilter filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _adminService.GetAllDisputesAsync(Caller.UserId, filter, request);
            return Ok(ApiResponse<PagedResult<DisputeListDto>>.Ok(result));
        }

        // GET /api/admin/disputes/open
        [HttpGet("disputes/open")]
        public async Task<ActionResult<ApiResponse<PagedResult<DisputeListDto>>>> GetOpenDisputes(
            [FromQuery] DisputeFilter filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _adminService.GetOpenDisputesAsync(Caller.UserId, filter, request);
            return Ok(ApiResponse<PagedResult<DisputeListDto>>.Ok(result));
        }

        // GET /api/admin/disputes/stats
        [HttpGet("disputes/stats")]
        public async Task<ActionResult<ApiResponse<DisputeStatsDto>>> GetDisputeStats()
        {
            var result = await _adminService.GetDisputeStatsAsync();
            return Ok(ApiResponse<DisputeStatsDto>.Ok(result));
        }

        // GET /api/admin/disputes/{disputeId}
        [HttpGet("disputes/{disputeId}")]
        public async Task<ActionResult<ApiResponse<DisputeDto>>> GetDisputeById(int disputeId)
        {
            var result = await _adminService.GetDisputeByIdAsync(disputeId, Caller.UserId);
            return Ok(ApiResponse<DisputeDto>.Ok(result));
        }

        // POST /api/admin/disputes/{disputeId}/resolve
        [HttpPost("disputes/{disputeId}/resolve")]
        public async Task<ActionResult<ApiResponse<DisputeDto>>> ResolveDispute(
            int disputeId,
            [FromBody] AdminResolveDisputeDto dto)
        {
            var result = await _adminService.ResolveDisputeAsync(disputeId, Caller.UserId, dto);
            return Ok(ApiResponse<DisputeDto>.Ok(result, "Dispute resolved successfully."));
        }

        //Fines

        // GET /api/admin/fines
        [HttpGet("fines")]
        public async Task<ActionResult<ApiResponse<PagedResult<FineListDto>>>> GetAllFines(
            [FromQuery] FineFilter filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _adminService.GetAllFinesAsync(filter, request);
            return Ok(ApiResponse<PagedResult<FineListDto>>.Ok(result));
        }

        // GET /api/admin/fines/{fineId}
        [HttpGet("fines/{fineId}")]
        public async Task<ActionResult<ApiResponse<FineDto>>> GetFineById(int fineId)
        {
            var result = await _adminService.GetFineByIdAsync(fineId);
            return Ok(ApiResponse<FineDto>.Ok(result));
        }

        // POST /api/admin/fines/{fineId}/void
        [HttpPost("fines/{fineId}/void")]
        public async Task<ActionResult<ApiResponse<string>>> VoidFine(int fineId)
        {
            await _adminService.VoidFineAsync(fineId, Caller.UserId);
            return Ok(ApiResponse<string>.Ok(null, "Fine voided successfully."));
        }

        // POST /api/admin/fines/{fineId}/approve-payment
        [HttpPost("fines/{fineId}/approve-payment")]
        public async Task<ActionResult<ApiResponse<FineDto>>> ApproveFinePayment(int fineId)
        {
            var result = await _adminService.ApproveFinePaymentAsync(fineId, Caller.UserId);
            return Ok(ApiResponse<FineDto>.Ok(result, "Payment approved successfully."));
        }

        // POST /api/admin/fines/{fineId}/reject-payment
        [HttpPost("fines/{fineId}/reject-payment")]
        public async Task<ActionResult<ApiResponse<FineDto>>> RejectFinePayment(
            int fineId,
            [FromBody] AdminRejectPaymentDto dto)
        {
            var result = await _adminService.RejectFinePaymentAsync(fineId, Caller.UserId, dto.Reason);
            return Ok(ApiResponse<FineDto>.Ok(result, "Payment rejected successfully."));
        }

        //Appeals 

        // GET /api/admin/appeals
        [HttpGet("appeals")]
        public async Task<ActionResult<ApiResponse<PagedResult<AppealDto>>>> GetAllAppeals(
            [FromQuery] AppealFilter filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _adminService.GetAllAppealsAsync(filter, request);
            return Ok(ApiResponse<PagedResult<AppealDto>>.Ok(result));
        }

        // GET /api/admin/appeals/{appealId}
        [HttpGet("appeals/{appealId}")]
        public async Task<ActionResult<ApiResponse<AppealDto>>> GetAppealById(int appealId)
        {
            var result = await _adminService.GetAppealByIdAsync(appealId);
            return Ok(ApiResponse<AppealDto>.Ok(result));
        }

        // POST /api/admin/appeals/{appealId}/decide/score
        [HttpPost("appeals/{appealId}/decide/score")]
        public async Task<ActionResult<ApiResponse<AppealDto>>> DecideScoreAppeal(
            int appealId,
            [FromBody] AdminDecidesScoreAppealDto dto)
        {
            var result = await _adminService.DecideScoreAppealAsync(appealId, Caller.UserId, dto);
            return Ok(ApiResponse<AppealDto>.Ok(result, "Score appeal decision recorded."));
        }

        // POST /api/admin/appeals/{appealId}/decide/fine
        [HttpPost("appeals/{appealId}/decide/fine")]
        public async Task<ActionResult<ApiResponse<AppealDto>>> DecideFineAppeal(
            int appealId,
            [FromBody] AdminDecidesFineAppealDto dto)
        {
            var result = await _adminService.DecideFineAppealAsync(appealId, Caller.UserId, dto);
            return Ok(ApiResponse<AppealDto>.Ok(result, "Fine appeal decision recorded."));
        }

        //Verifications

        // GET /api/admin/verifications
        [HttpGet("verifications")]
        public async Task<ActionResult<ApiResponse<PagedResult<VerificationRequestDto>>>> GetAllVerifications(
            [FromQuery] VerificationRequestFilter filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _adminService.GetAllVerificationsAsync(filter, request);
            return Ok(ApiResponse<PagedResult<VerificationRequestDto>>.Ok(result));
        }

        // GET /api/admin/verifications/{verificationId}
        [HttpGet("verifications/{verificationId}")]
        public async Task<ActionResult<ApiResponse<VerificationRequestDto>>> GetVerificationById(int verificationId)
        {
            var result = await _adminService.GetVerificationByIdAsync(verificationId, Caller.UserId);
            return Ok(ApiResponse<VerificationRequestDto>.Ok(result));
        }

        // POST /api/admin/verifications/{verificationId}/approve
        [HttpPost("verifications/{verificationId}/approve")]
        public async Task<ActionResult<ApiResponse<VerificationRequestDto>>> ApproveVerification(int verificationId)
        {
            var result = await _adminService.ApproveVerificationAsync(verificationId, Caller.UserId);
            return Ok(ApiResponse<VerificationRequestDto>.Ok(result, "Verification approved successfully."));
        }

        // POST /api/admin/verifications/{verificationId}/reject
        [HttpPost("verifications/{verificationId}/reject")]
        public async Task<ActionResult<ApiResponse<VerificationRequestDto>>> RejectVerification(
            int verificationId,
            [FromBody] AdminRejectDto dto)
        {
            var result = await _adminService.RejectVerificationAsync(verificationId, Caller.UserId, dto.Reason);
            return Ok(ApiResponse<VerificationRequestDto>.Ok(result, "Verification rejected successfully."));
        }

        // Reports

        // GET /api/admin/reports
        [HttpGet("reports")]
        public async Task<ActionResult<ApiResponse<PagedResult<ReportDto>>>> GetAllReports(
            [FromQuery] ReportFilter filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _adminService.GetAllReportsAsync(filter, request);
            return Ok(ApiResponse<PagedResult<ReportDto>>.Ok(result));
        }

        // GET /api/admin/reports/{reportId}
        [HttpGet("reports/{reportId}")]
        public async Task<ActionResult<ApiResponse<ReportDto>>> GetReportById(int reportId)
        {
            var result = await _adminService.GetReportByIdAsync(reportId, Caller.UserId);
            return Ok(ApiResponse<ReportDto>.Ok(result));
        }

        // POST /api/admin/reports/{reportId}/resolve
        [HttpPost("reports/{reportId}/resolve")]
        public async Task<ActionResult<ApiResponse<ReportDto>>> ResolveReport(
            int reportId,
            [FromBody] AdminResolveReportDto dto)
        {
            var result = await _adminService.ResolveReportAsync(reportId, Caller.UserId, dto);
            return Ok(ApiResponse<ReportDto>.Ok(result, "Report resolved successfully."));
        }

        // POST /api/admin/reports/{reportId}/dismiss
        [HttpPost("reports/{reportId}/dismiss")]
        public async Task<ActionResult<ApiResponse<ReportDto>>> DismissReport(
            int reportId,
            [FromBody] AdminRejectDto dto)
        {
            var result = await _adminService.DismissReportAsync(reportId, Caller.UserId, dto.Reason);
            return Ok(ApiResponse<ReportDto>.Ok(result, "Report dismissed successfully."));
        }

        //Support

        // GET /api/admin/support
        [HttpGet("support")]
        public async Task<ActionResult<ApiResponse<PagedResult<SupportThreadListDto>>>> GetAllSupportThreads(
            [FromQuery] SupportThreadFilter filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _adminService.GetAllSupportThreadsAsync(filter, request);
            return Ok(ApiResponse<PagedResult<SupportThreadListDto>>.Ok(result));
        }

        // GET /api/admin/support/{threadId}
        [HttpGet("support/{threadId}")]
        public async Task<ActionResult<ApiResponse<SupportThreadDto>>> GetSupportThreadById(int threadId)
        {
            var result = await _adminService.GetSupportThreadByIdAsync(threadId, Caller.UserId);
            return Ok(ApiResponse<SupportThreadDto>.Ok(result));
        }

        // POST /api/admin/support/{threadId}/claim
        [HttpPost("support/{threadId}/claim")]
        public async Task<ActionResult<ApiResponse<SupportThreadDto>>> ClaimSupportThread(int threadId)
        {
            var result = await _adminService.ClaimSupportThreadAsync(threadId, Caller.UserId);
            return Ok(ApiResponse<SupportThreadDto>.Ok(result, "Thread claimed successfully."));
        }

        // POST /api/admin/support/{threadId}/close
        [HttpPost("support/{threadId}/close")]
        public async Task<ActionResult<ApiResponse<string>>> CloseSupportThread(int threadId)
        {
            await _adminService.CloseSupportThreadAsync(threadId, Caller.UserId);
            return Ok(ApiResponse<string>.Ok(null, "Thread closed successfully."));
        }

        // POST /api/admin/support/{threadId}/message
        [HttpPost("support/{threadId}/message")]
        public async Task<ActionResult<ApiResponse<SupportMessageDto>>> SendSupportMessage(
            int threadId,
            [FromBody] SendSupportMessageDto dto)
        {
            var result = await _adminService.SendSupportMessageAsync(threadId, Caller.UserId, dto);
            return Ok(ApiResponse<SupportMessageDto>.Ok(result));
        }

        //Notifications

        // POST /api/admin/notifications/send
        [HttpPost("notifications/send")]
        public async Task<ActionResult<ApiResponse<string>>> SendSystemNotification(
            [FromBody] AdminSendNotificationDto dto)
        {
            await _adminService.SendSystemNotificationAsync(dto.UserId, dto.Message, dto.Type);
            return Ok(ApiResponse<string>.Ok(null, "Notification sent successfully."));
        }

        // POST /api/admin/notifications/broadcast
        [HttpPost("notifications/broadcast")]
        public async Task<ActionResult<ApiResponse<string>>> BroadcastNotification(
            [FromBody] AdminBroadcastNotificationDto dto)
        {
            await _adminService.BroadcastNotificationAsync(dto.Message, dto.Type);
            return Ok(ApiResponse<string>.Ok(null, "Broadcast sent successfully."));
        }
    }
}