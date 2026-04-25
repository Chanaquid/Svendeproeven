using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using backend.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminUserService _adminUserService;
        private readonly IAdminUserRepository _adminUserRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IItemService _itemService;
        private readonly ILoanRepository _loanRepository;
        private readonly ILoanService _loanService;
        private readonly IDisputeRepository _disputeRepository;
        private readonly IDisputeService _disputeService;
        private readonly IFineRepository _fineRepository;
        private readonly IFineService _fineService;
        private readonly IAppealRepository _appealRepository;
        private readonly IAppealService _appealService;
        private readonly IVerificationRequestRepository _verificationRequestRepository;
        private readonly IVerificationRequestService _verificationRequestService;
        private readonly IReportRepository _reportRepository;
        private readonly IReportService _reportService;
        private readonly ISupportService _supportService;
        private readonly INotificationService _notificationService;
        private readonly IUserBanHistoryService _userBanHistoryService;
        private readonly IDirectConversationRepository _conversationRepository;
        private readonly IScoreHistoryService _scoreHistoryService;
        private readonly ISupportRepository _supportRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminService(
            IAdminUserService adminUserService,
            IAdminUserRepository adminUserRepository,
            IItemRepository itemRepository,
            IItemService itemService,
            ILoanRepository loanRepository,
            ILoanService loanService,
            IDisputeRepository disputeRepository,
            IDisputeService disputeService,
            IFineRepository fineRepository,
            IFineService fineService,
            IAppealRepository appealRepository,
            IAppealService appealService,
            IVerificationRequestRepository verificationRequestRepository,
            IVerificationRequestService verificationRequestService,
            IReportRepository reportRepository,
            IReportService reportService,
            ISupportService supportService,
            INotificationService notificationService,
            IUserBanHistoryService userBanHistoryService,
            IScoreHistoryService scoreHistoryService,
            IDirectConversationRepository conversationRepository,
            ISupportRepository supportRepository,
            UserManager<ApplicationUser> userManager)
        {
            _adminUserService = adminUserService;
            _adminUserRepository = adminUserRepository;
            _itemRepository = itemRepository;
            _itemService = itemService;
            _loanRepository = loanRepository;
            _loanService = loanService;
            _disputeRepository = disputeRepository;
            _disputeService = disputeService;
            _fineRepository = fineRepository;
            _fineService = fineService;
            _appealRepository = appealRepository;
            _appealService = appealService;
            _verificationRequestRepository = verificationRequestRepository;
            _verificationRequestService = verificationRequestService;
            _reportRepository = reportRepository;
            _reportService = reportService;
            _supportService = supportService;
            _notificationService = notificationService;
            _scoreHistoryService = scoreHistoryService;
            _userBanHistoryService = userBanHistoryService;
            _conversationRepository = conversationRepository;
            _userManager = userManager;
            _supportRepository = supportRepository;
        }


        //Dashboard

        public async Task<AdminDashboardDto> GetDashboardAsync()
        {
            var now = DateTime.UtcNow;
            var weekAgo = now.AddDays(-7);
            var monthAgo = now.AddMonths(-1);

            // Action queues
            var pendingItems = await _itemRepository.CountAsync(new ItemFilter { Status = ItemStatus.Pending });
            var pendingLoans = await _loanRepository.CountAsync(new LoanFilter { Status = LoanStatus.Pending });
            var openDisputes = await _disputeRepository.CountAsync(new DisputeFilter { Status = DisputeStatus.PendingAdminReview });
            var overdueDisputes = await _disputeRepository.CountAsync(new DisputeFilter { Status = DisputeStatus.PastDeadline });
            var pendingAppeals = await _appealRepository.CountAsync(new AppealFilter { Status = AppealStatus.Pending });
            var pendingVerif = await _verificationRequestRepository.CountAsync(new VerificationRequestFilter { Status = VerificationStatus.Pending });
            var pendingPayments = await _fineRepository.CountAsync(new FineFilter { Status = FineStatus.PendingVerification });
            var pendingReports = await _reportRepository.CountAsync(new ReportFilter { Status = ReportStatus.Pending });
            var pendingSupport = await _supportRepository.CountAsync(new SupportThreadFilter { Status = SupportThreadStatus.Open});


            // Platform stats
            var totalUsers = await _userManager.Users.CountAsync();
            var verifiedUsers = await _verificationRequestRepository.CountAsync(new VerificationRequestFilter { Status = VerificationStatus.Approved });
            var bannedUsers = await _userManager.Users.CountAsync(u => u.IsBanned);
            var newUsersWeek = await _userManager.Users.CountAsync(u => u.CreatedAt >= weekAgo);

            // Items
            var availableItems = await _itemRepository.CountAsync(new ItemFilter { Status = ItemStatus.Approved, Availability = ItemAvailability.Available });
            var onRentItems = await _itemRepository.CountAsync(new ItemFilter { Status = ItemStatus.Approved, Availability = ItemAvailability.OnRent });
            var itemsWeek = await _itemRepository.CountAsync(new ItemFilter { CreatedAfter = weekAgo });

            // Loans
            var activeLoans = await _loanRepository.CountAsync(new LoanFilter { Status = LoanStatus.Active });
            var overdueLoans = await _loanRepository.CountAsync(new LoanFilter { Status = LoanStatus.Late });
            var loansWeek = await _loanRepository.CountAsync(new LoanFilter { CreatedAfter = weekAgo });

            // Fines
            var unpaidFines = await _fineRepository.CountAsync(new FineFilter { Status = FineStatus.Unpaid });
            var unpaidAmount = await _fineRepository.SumAmountAsync(new FineFilter { Status = FineStatus.Unpaid });
            var collectedMonth = await _fineRepository.SumAmountAsync(new FineFilter { Status = FineStatus.Paid, PaidAfter = monthAgo });
            var issuedMonth = await _fineRepository.CountAsync(new FineFilter { CreatedAfter = monthAgo });

            // Disputes
            var resolvedDisputes = await _disputeRepository.CountAsync(new DisputeFilter { Status = DisputeStatus.Resolved, ResolvedAfter = monthAgo });
            var avgResolution = await _disputeRepository.GetAverageResolutionDaysAsync();

            return new AdminDashboardDto
            {
                PendingItemApprovals = pendingItems,
                PendingLoanApprovals = pendingLoans,
                OpenDisputes = openDisputes,
                OverdueDisputeResponses = overdueDisputes,
                PendingAppeals = pendingAppeals,
                PendingUserVerifications = pendingVerif,
                PendingReports = pendingReports,
                PendingFines = pendingPayments,
                PendingSupports = pendingSupport,
                TotalUsers = totalUsers,
                VerifiedUsers = verifiedUsers,
                BannedUsers = bannedUsers,
                NewUsersThisWeek = newUsersWeek,
                TotalActiveItems = availableItems + onRentItems,
                ItemsListedThisWeek = itemsWeek,
                TotalActiveLoans = activeLoans,
                OverdueLoans = overdueLoans,
                LoansCreatedThisWeek = loansWeek,
                TotalUnpaidFines = unpaidFines,
                TotalUnpaidFinesAmount = unpaidAmount,
                FinesCollectedThisMonth = collectedMonth,
                FinesIssuedThisMonth = issuedMonth,
                DisputesResolvedThisMonth = resolvedDisputes,
                AverageDisputeResolutionDays = avgResolution,
            };
        }


        //Users

        public async Task<PagedResult<UserProfileDto>> GetAllUsersAsync(UserFilter filter, PagedRequest request)
        {
            var result = await _adminUserRepository.GetAllUsersIncludingDeletedAsync(filter, request);

            return new PagedResult<UserProfileDto>
            {
                Items = result.Items.Select(x => new UserProfileDto
                {
                    Id = x.User.Id,
                    FullName = x.User.FullName,
                    Username = x.User.UserName ?? string.Empty,
                    Email = x.User.Email ?? string.Empty,
                    PhoneNumber = x.User.PhoneNumber,
                    Gender = x.User.Gender,
                    Role = x.Role,
                    Bio = x.User.Bio,
                    Address = x.User.Address,
                    Latitude = x.User.Latitude,
                    Longitude = x.User.Longitude,
                    DateOfBirth = x.User.DateOfBirth,
                    Age = DateTime.UtcNow.Year - x.User.DateOfBirth.Year,
                    AvatarUrl = x.User.AvatarUrl,
                    Score = x.User.Score,
                    IsVerified = x.User.IsVerified,
                    IsBanned = x.User.IsBanned,
                    MembershipDate = x.User.MembershipDate,
                    CreatedAt = x.User.CreatedAt,
                    UpdatedAt = x.User.UpdatedAt
                }).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<AdminUserDto> GetAdminUserByIdAsync(string userId)
        {
            return await _adminUserService.GetUserByIdWIthDetailsAsync(userId);
        }

        public async Task<AdminUserDto> AdminEditUserAsync(string userId, string adminId, AdminEditUserDto dto)
        {
            return await _adminUserService.AdminEditUserAsync(userId, adminId, dto);
        }


        public async Task<AdminDeleteResultDto> AdminSoftDeleteUserAsync(string userId, string adminId, string? note = null)
        {
            return await _adminUserService.AdminSoftDeleteUserAsync(userId, adminId, note);
        }



        public async Task BanUserAsync(string userId, string adminId, BanUserDto dto)
        {
            await _adminUserService.BanUserAsync(userId, adminId, dto);
        }

        public async Task UnbanUserAsync(string userId, string adminId, UnbanUserDto dto)
        {
            await _adminUserService.UnbanUserAsync(userId, adminId, dto);
        }

        public async Task<PagedResult<UserBanHistoryDto>> GetBanHistoryAsync(string userId, UserBanHistoryFilter? filter, PagedRequest request)
        {
            return await _userBanHistoryService.GetByUserIdAsync(userId, filter, request);
        }

        public async Task<PagedResult<UserBanHistoryDto>> GetAllBansAsync(UserBanHistoryFilter? filter, PagedRequest request)
        {
            return await _userBanHistoryService.GetAllAsync(filter, request);
        }

        public async Task AdjustUserScoreAsync(string adminId, AdminAdjustScoreDto dto)
        {
            await _scoreHistoryService.AdminAdjustScoreAsync(adminId, dto);
        }

        //Items

        public async Task<PagedResult<ItemListDto>> GetAllItemsAsync(ItemFilter filter, PagedRequest request)
        {
            return await _itemService.AdminGetAllAsync(filter, request);
        }

        public async Task<ItemDto> GetItemByIdAsync(int itemId)
        {
            return await _itemService.AdminGetByIdAsync(itemId);
        }

        public async Task ApproveItemAsync(int itemId, string adminId)
        {
            await _itemService.DecideItemAsync(adminId, itemId, new AdminDecideItemDto { IsApproved = true });
        }

        public async Task RejectItemAsync(int itemId, string adminId, string reason)
        {
            await _itemService.DecideItemAsync(adminId, itemId, new AdminDecideItemDto { IsApproved = false, AdminNote = reason });
        }

        public async Task DeleteItemAsync(int itemId, string adminId)
        {
            await _itemService.DeleteItemAsync(adminId, itemId);
        }

        //Loans

        public async Task<PagedResult<LoanListDto>> GetAllLoansAsync(LoanFilter filter, PagedRequest request)
        {
            return await _loanService.AdminGetAllAsync(filter, request);
        }

        public async Task<LoanDto> GetLoanByIdAsync(int loanId)
        {
            return await _loanService.AdminGetByIdAsync(loanId);
        }

        public async Task ForceCancelLoanAsync(int loanId, string adminId, string reason)
        {
            await _loanService.ForceCancelLoanAsync(loanId, adminId, reason);
        }


        //Disputes

        public async Task<PagedResult<DisputeListDto>> GetAllDisputesAsync(string adminId, DisputeFilter filter, PagedRequest request)
        {
            return await _disputeService.GetAllDisputesAsync(adminId, filter, request);
        }

        public async Task<PagedResult<DisputeListDto>> GetOpenDisputesAsync(string adminId, DisputeFilter filter, PagedRequest request)
        {
            return await _disputeService.GetAllOpenDisputesAsync(adminId, filter, request);
        }

        public async Task<DisputeDto> GetDisputeByIdAsync(int disputeId, string adminId)
        {
            return await _disputeService.AdminGetDisputeByIdAsync(disputeId, adminId);
        }

        public async Task<DisputeDto> ResolveDisputeAsync(int disputeId, string adminId, AdminResolveDisputeDto dto)
        {
            return await _disputeService.ResolveDisputeAsync(adminId, disputeId, dto);
        }

        public async Task<DisputeStatsDto> GetDisputeStatsAsync()
        {
            return await _disputeService.GetDisputeStatsAsync();
        }

        //Fines

        public async Task<PagedResult<FineListDto>> GetAllFinesAsync(FineFilter filter, PagedRequest request)
        {
            return await _fineService.GetAllFinesAsync(filter, request);
        }

        public async Task<FineDto> GetFineByIdAsync(int fineId)
        {
            return await _fineService.AdminGetFineByIdAsync(fineId);
        }

        public async Task VoidFineAsync(int fineId, string adminId)
        {
            await _fineService.VoidFineAsync(adminId, fineId);
        }

        public async Task<FineDto> ApproveFinePaymentAsync(int fineId, string adminId)
        {
            return await _fineService.VerifyPaymentAsync(adminId, fineId, new AdminFineVerifyPaymentDto { IsApproved = true });
        }

        public async Task<FineDto> RejectFinePaymentAsync(int fineId, string adminId, string reason)
        {
            return await _fineService.VerifyPaymentAsync(adminId, fineId, new AdminFineVerifyPaymentDto { IsApproved = false, RejectionReason = reason });
        }


        //Appeals

        public async Task<PagedResult<AppealDto>> GetAllAppealsAsync(AppealFilter filter, PagedRequest request)
        {
            return await _appealService.GetAllAppealsAsync(filter, request);
        }

        public async Task<AppealDto> GetAppealByIdAsync(int appealId)
        {
            return await _appealService.GetByIdWithDetailsAsync(appealId);
        }

        public async Task<AppealDto> DecideScoreAppealAsync(int appealId, string adminId, AdminDecidesScoreAppealDto dto)
        {
            return await _appealService.DecideScoreAppealAsync(appealId, adminId, dto);
        }

        public async Task<AppealDto> DecideFineAppealAsync(int appealId, string adminId, AdminDecidesFineAppealDto dto)
        {
            return await _appealService.DecideFineAppealAsync(appealId, adminId, dto);
        }

        //Verification requests

        public async Task<PagedResult<VerificationRequestDto>> GetAllVerificationsAsync(VerificationRequestFilter filter, PagedRequest request)
        {
            return await _verificationRequestService.GetAllAsync(filter, request);
        }

        public async Task<VerificationRequestDto> GetVerificationByIdAsync(int verificationId, string adminId)
        {
            return await _verificationRequestService.GetByIdAsync(verificationId, adminId, isAdmin: true);
        }

        public async Task<VerificationRequestDto> ApproveVerificationAsync(int verificationId, string adminId)
        {
            return await _verificationRequestService.DecideAsync(verificationId, adminId, new AdminDecideVerificationRequestDto
            {
                Status = VerificationStatus.Approved
            });
        }

        public async Task<VerificationRequestDto> RejectVerificationAsync(int verificationId, string adminId, string reason)
        {
            return await _verificationRequestService.DecideAsync(verificationId, adminId, new AdminDecideVerificationRequestDto
            {
                Status = VerificationStatus.Rejected,
                AdminNote = reason
            });
        }

        //Reports

        public async Task<PagedResult<ReportDto>> GetAllReportsAsync(ReportFilter filter, PagedRequest request)
        {
            return await _reportService.GetAllAsync(filter, request);
        }

        public async Task<ReportDto> GetReportByIdAsync(int reportId, string adminId)
        {
            return await _reportService.GetByIdAsync(reportId, adminId, isAdmin: true);
        }

        public async Task<ReportDto> ResolveReportAsync(int reportId, string adminId, AdminResolveReportDto dto)
        {
            return await _reportService.ResolveReportAsync(reportId, adminId, dto);
        }

        public async Task<ReportDto> DismissReportAsync(int reportId, string adminId, string reason)
        {
            return await _reportService.ResolveReportAsync(reportId, adminId, new AdminResolveReportDto
            {
                Status = ReportStatus.Dismissed,
                AdminNote = reason
            });
        }

        //Support

        public async Task<PagedResult<SupportThreadListDto>> GetAllSupportThreadsAsync(SupportThreadFilter filter, PagedRequest request)
        {
            return await _supportService.GetAllThreadsAsync(filter, request);
        }

        public async Task<SupportThreadDto> GetSupportThreadByIdAsync(int threadId, string adminId)
        {
            return await _supportService.GetThreadByIdAsync(threadId, adminId, isAdmin: true);
        }

        public async Task<SupportThreadDto> ClaimSupportThreadAsync(int threadId, string adminId)
        {
            return await _supportService.ClaimThreadAsync(threadId, adminId);
        }

        public async Task CloseSupportThreadAsync(int threadId, string adminId)
        {
            await _supportService.CloseThreadAsync(threadId, adminId, isAdmin: true);
        }

        public async Task<SupportMessageDto> SendSupportMessageAsync(int threadId, string adminId, SendSupportMessageDto dto)
        {
            return await _supportService.SendMessageAsync(threadId, adminId, dto, isAdmin: true);
        }

        //Notifications

        public async Task SendSystemNotificationAsync(string userId, string message, NotificationType type)
        {
            await _notificationService.SendAsync(
                userId,
                type,
                message,
                referenceId: null,
                referenceType: null);
        }

        public async Task BroadcastNotificationAsync(string message, NotificationType type)
        {
            var users = _userManager.Users.Select(u => u.Id).ToList();
            await _notificationService.SendToMultipleAsync(users, type, message);
        }

        //Helpers

        private static UserProfileDto MapToProfileDto(ApplicationUser user) => new()
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            AvatarUrl = user.AvatarUrl,
            Score = user.Score,
            IsBanned = user.IsBanned,
            CreatedAt = user.CreatedAt,
        };
    }
}