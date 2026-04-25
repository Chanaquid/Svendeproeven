using backend.Dtos;
using backend.Models;

public interface IAdminService
{
    // Dashboard
    Task<AdminDashboardDto> GetDashboardAsync();

    // Users
    Task<PagedResult<UserProfileDto>> GetAllUsersAsync(UserFilter filter, PagedRequest request);
    Task<UserProfileDto> GetUserByIdAsync(string userId);
    Task BanUserAsync(string userId, string adminId, BanUserDto dto);
    Task UnbanUserAsync(string userId, string adminId, UnbanUserDto dto);
    Task<PagedResult<UserBanHistoryDto>> GetBanHistoryAsync(string userId, UserBanHistoryFilter? filter, PagedRequest request);
    Task<PagedResult<UserBanHistoryDto>> GetAllBansAsync(UserBanHistoryFilter? filter, PagedRequest request);
    Task AdjustUserScoreAsync(string adminId, AdminAdjustScoreDto dto);

    // Items
    Task<PagedResult<ItemListDto>> GetAllItemsAsync(ItemFilter filter, PagedRequest request);
    Task<ItemDto> GetItemByIdAsync(int itemId);
    Task ApproveItemAsync(int itemId, string adminId);
    Task RejectItemAsync(int itemId, string adminId, string reason);
    Task DeleteItemAsync(int itemId, string adminId);

    // Loans
    Task<PagedResult<LoanListDto>> GetAllLoansAsync(LoanFilter filter, PagedRequest request);
    Task<LoanDto> GetLoanByIdAsync(int loanId);
    Task ForceCancelLoanAsync(int loanId, string adminId, string reason);

    // Disputes
    Task<PagedResult<DisputeListDto>> GetAllDisputesAsync(string adminId, DisputeFilter filter, PagedRequest request);
    Task<PagedResult<DisputeListDto>> GetOpenDisputesAsync(string adminId, DisputeFilter filter, PagedRequest request);
    Task<DisputeDto> GetDisputeByIdAsync(int disputeId, string adminId);
    Task<DisputeDto> ResolveDisputeAsync(int disputeId, string adminId, AdminResolveDisputeDto dto);
    Task<DisputeStatsDto> GetDisputeStatsAsync();

    // Fines
    Task<PagedResult<FineListDto>> GetAllFinesAsync(FineFilter filter, PagedRequest request);
    Task<FineDto> GetFineByIdAsync(int fineId);
    Task VoidFineAsync(int fineId, string adminId);
    Task<FineDto> ApproveFinePaymentAsync(int fineId, string adminId);
    Task<FineDto> RejectFinePaymentAsync(int fineId, string adminId, string reason);

    // Appeals
    Task<PagedResult<AppealDto>> GetAllAppealsAsync(AppealFilter filter, PagedRequest request);
    Task<AppealDto> GetAppealByIdAsync(int appealId);
    Task<AppealDto> DecideScoreAppealAsync(int appealId, string adminId, AdminDecidesScoreAppealDto dto);
    Task<AppealDto> DecideFineAppealAsync(int appealId, string adminId, AdminDecidesFineAppealDto dto);

    // Verifications
    Task<PagedResult<VerificationRequestDto>> GetAllVerificationsAsync(VerificationRequestFilter filter, PagedRequest request);
    Task<VerificationRequestDto> GetVerificationByIdAsync(int verificationId, string adminId);
    Task<VerificationRequestDto> ApproveVerificationAsync(int verificationId, string adminId);
    Task<VerificationRequestDto> RejectVerificationAsync(int verificationId, string adminId, string reason);

    // Reports
    Task<PagedResult<ReportDto>> GetAllReportsAsync(ReportFilter filter, PagedRequest request);
    Task<ReportDto> GetReportByIdAsync(int reportId, string adminId);
    Task<ReportDto> ResolveReportAsync(int reportId, string adminId, AdminResolveReportDto dto);
    Task<ReportDto> DismissReportAsync(int reportId, string adminId, string reason);

    // Support
    Task<PagedResult<SupportThreadListDto>> GetAllSupportThreadsAsync(SupportThreadFilter filter, PagedRequest request);
    Task<SupportThreadDto> GetSupportThreadByIdAsync(int threadId, string adminId);
    Task<SupportThreadDto> ClaimSupportThreadAsync(int threadId, string adminId);
    Task CloseSupportThreadAsync(int threadId, string adminId);
    Task<SupportMessageDto> SendSupportMessageAsync(int threadId, string adminId, SendSupportMessageDto dto);

    // Notifications
    Task SendSystemNotificationAsync(string userId, string message, NotificationType type);
    Task BroadcastNotificationAsync(string message, NotificationType type);
}