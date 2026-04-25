using backend.Common;
using backend.Dtos;
using backend.Interfaces;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    [ApiController]
    [Route("api/admin/users")]
    public class AdminUserController : BaseController
    {
        private readonly IAdminUserService _adminUserService;
        private readonly IItemService _itemService;
        private readonly ILoanService _loanService;
        private readonly IAppealService _appealService;
        private readonly IDisputeService _disputeService;
        private readonly IScoreHistoryService _scoreHistoryService;
        private readonly IVerificationRequestService _verificationRequestService;
        private readonly ISupportService _supportService;
        private readonly IFineService _fineService;
        private readonly IReportService _reportService;


        public AdminUserController(
            IAdminUserService adminUserService,
            IItemService itemService,
            IAppealService appealService,
            ILoanService loanService,
            IDisputeService disputeService,
            IScoreHistoryService scoreHistoryService,
            IVerificationRequestService verificationService,
            ISupportService supportService,
            IFineService fineService,
            IReportService reportService)
        {
            _adminUserService = adminUserService;
            _itemService = itemService;
            _appealService = appealService;
            _loanService = loanService;
            _disputeService = disputeService;
            _scoreHistoryService = scoreHistoryService;
            _verificationRequestService = verificationService;
            _supportService = supportService;
            _fineService = fineService;
            _reportService = reportService;
        }

        // GET /api/admin/users
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<AdminUserDto>>>> GetAllUsers(
            [FromQuery] UserFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _adminUserService.GetUsersAsync(filter, request);
            return Ok(ApiResponse<PagedResult<AdminUserDto>>.Ok(result));
        }

        // GET /api/admin/users/all
        [HttpGet("all")]
        public async Task<ActionResult<ApiResponse<PagedResult<AdminUserDto>>>> GetAllIncludingDeleted(
            [FromQuery] UserFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _adminUserService.GetAllUsersIncludingDeletedAsync(filter, request);
            return Ok(ApiResponse<PagedResult<AdminUserDto>>.Ok(result));
        }

        // GET /api/admin/users/banned
        [HttpGet("banned")]
        public async Task<ActionResult<ApiResponse<PagedResult<AdminUserDto>>>> GetAllBannedUsers(
            [FromQuery] UserFilter? filter,
            [FromQuery] PagedRequest request,
            [FromQuery] bool tempBansOnly = false)
        {
            var result = await _adminUserService.GetAllBannedUsersAsync(filter, request, tempBansOnly);
            return Ok(ApiResponse<PagedResult<AdminUserDto>>.Ok(result));
        }

        // GET /api/admin/users/{userId}
        [HttpGet("{userId}")]
        public async Task<ActionResult<ApiResponse<AdminUserDto>>> GetUserById(string userId)
        {
            var result = await _adminUserService.GetUserByIdWIthDetailsAsync(userId);
            return Ok(ApiResponse<AdminUserDto>.Ok(result));
        }

        // GET /api/admin/users/{userId}/items
        [HttpGet("{userId}/items")]
        public async Task<ActionResult<ApiResponse<PagedResult<ItemListDto>>>> GetUserItems(
            string userId,
            [FromQuery] ItemFilter? filter,
            [FromQuery] PagedRequest request)
        {
            filter ??= new ItemFilter();
            filter.OwnerId = userId; 
            var result = await _itemService.AdminGetAllAsync(filter, request);
            return Ok(ApiResponse<PagedResult<ItemListDto>>.Ok(result));
        }

        // GET /api/admin/users/{userId}/loans
        [HttpGet("{userId}/loans")]
        public async Task<ActionResult<ApiResponse<PagedResult<LoanListDto>>>> GetUserLoans(
            string userId,
            [FromQuery] LoanFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _loanService.GetAllLoansByUserIdAsync(userId, filter, request, isAdmin: true);
            return Ok(ApiResponse<PagedResult<LoanListDto>>.Ok(result));
        }

        // GET /api/admin/users/{userId}/fines
        [HttpGet("{userId}/fines")]
        public async Task<ActionResult<ApiResponse<PagedResult<FineListDto>>>> GetUserFines(
            string userId,
            [FromQuery] FineFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _fineService.GetAllFinesByUserIdAsync(userId, filter, request, isAdmin: true);
            return Ok(ApiResponse<PagedResult<FineListDto>>.Ok(result));
        }

        // GET /api/admin/users/{userId}/score-history
        [HttpGet("{userId}/score-history")]
        public async Task<ActionResult<ApiResponse<PagedResult<ScoreHistoryDto>>>> GetUserScoreHistory(
            string userId,
            [FromQuery] ScoreHistoryFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _scoreHistoryService.GetByUserIdAsync(userId, filter, request);
            return Ok(ApiResponse<PagedResult<ScoreHistoryDto>>.Ok(result));
        }

        // GET /api/admin/users/{userId}/appeals
        [HttpGet("{userId}/appeals")]
        public async Task<ActionResult<ApiResponse<PagedResult<AppealDto>>>> GetUserAppeals(
            string userId,
            [FromQuery] AppealFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _appealService.GetAllAppealsByUserIdAsync(userId, filter, request);
            return Ok(ApiResponse<PagedResult<AppealDto>>.Ok(result));
        }

        // GET /api/admin/users/{userId}/disputes
        [HttpGet("{userId}/disputes")]
        public async Task<ActionResult<ApiResponse<PagedResult<DisputeListDto>>>> GetUserDisputes(
            string userId,
            [FromQuery] DisputeFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _disputeService.GetAllDisputesByUserIdAsync(userId, filter, request);
            return Ok(ApiResponse<PagedResult<DisputeListDto>>.Ok(result));
        }

        // GET /api/admin/users/{userId}/verifications
        [HttpGet("{userId}/verifications")]
        public async Task<ActionResult<ApiResponse<PagedResult<VerificationRequestDto>>>> GetUserVerifications(
            string userId,
            [FromQuery] VerificationRequestFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _verificationRequestService.GetByUserIdAsync(userId, filter, request);
            return Ok(ApiResponse<PagedResult<VerificationRequestDto>>.Ok(result));
        }

        // GET /api/admin/users/{userId}/reports
        [HttpGet("{userId}/reports")]
        public async Task<ActionResult<ApiResponse<PagedResult<ReportDto>>>> GetUserReports(
            string userId,
            [FromQuery] ReportFilter? filter,
            [FromQuery] PagedRequest request)
        {
            filter ??= new ReportFilter();
            filter.ReportedById = userId;
            var result = await _reportService.GetAllAsync(filter, request);
            return Ok(ApiResponse<PagedResult<ReportDto>>.Ok(result));
        }

        // GET /api/admin/users/{userId}/support-threads
        [HttpGet("{userId}/support-threads")]
        public async Task<ActionResult<ApiResponse<PagedResult<SupportThreadListDto>>>> GetUserSupportThreads(
            string userId,
            [FromQuery] SupportThreadFilter? filter,
            [FromQuery] PagedRequest request)
        {
            filter ??= new SupportThreadFilter();
            filter.UserId = userId;
            var result = await _supportService.GetAllThreadsAsync(filter, request);
            return Ok(ApiResponse<PagedResult<SupportThreadListDto>>.Ok(result));
        }

        // PUT /api/admin/users/{userId}
        [HttpPut("{userId}")]
        public async Task<ActionResult<ApiResponse<AdminUserDto>>> UpdateUser(
            string userId,
            [FromBody] AdminEditUserDto dto)
        {
            var result = await _adminUserService.AdminEditUserAsync(userId, Caller.UserId, dto);
            return Ok(ApiResponse<AdminUserDto>.Ok(result, "User updated successfully."));
        }

        // POST /api/admin/users/{userId}/score
        [HttpPost("{userId}/score")]
        public async Task<ActionResult<ApiResponse<string>>> AdjustScore(
            string userId,
            [FromBody] AdminAdjustScoreDto dto)
        {
            // Inject userId into dto since admin is acting on a target user
            dto.UserId = userId;
            await _scoreHistoryService.AdminAdjustScoreAsync(Caller.UserId, dto);
            return Ok(ApiResponse<string>.Ok(null, "Score adjusted successfully."));
        }

        // POST /api/admin/users/{userId}/ban
        [HttpPost("{userId}/ban")]
        public async Task<ActionResult<ApiResponse<string>>> BanUser(
            string userId,
            [FromBody] BanUserDto dto)
        {
            await _adminUserService.BanUserAsync(userId, Caller.UserId, dto);
            return Ok(ApiResponse<string>.Ok(null, "User banned successfully."));
        }

        // POST /api/admin/users/{userId}/unban
        [HttpPost("{userId}/unban")]
        public async Task<ActionResult<ApiResponse<string>>> UnbanUser(
            string userId,
            [FromBody] UnbanUserDto dto)
        {
            await _adminUserService.UnbanUserAsync(userId, Caller.UserId, dto);
            return Ok(ApiResponse<string>.Ok(null, "User unbanned successfully."));
        }

        // DELETE /api/admin/users/{userId}
        [HttpDelete("{userId}")]
        public async Task<ActionResult<ApiResponse<AdminDeleteResultDto>>> DeleteUser(
            string userId,
            [FromQuery] string? note)
        {
            var result = await _adminUserService.AdminSoftDeleteUserAsync(userId, Caller.UserId, note);
            return Ok(ApiResponse<AdminDeleteResultDto>.Ok(result, "User deleted successfully."));
        }
    }
}