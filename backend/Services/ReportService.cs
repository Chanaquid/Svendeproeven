using backend.Dtos;
using backend.Interfaces;
using backend.Models;

namespace backend.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;
        private readonly IUserRepository _userRepository;

        private const int ReportCooldownMinutes = 60;

        public ReportService(
            IReportRepository reportRepository,
            IUserRepository userRepository)
        {
            _reportRepository = reportRepository;
            _userRepository = userRepository;
        }

        //User actions

        public async Task<ReportDto> CreateReportAsync(string userId, CreateReportDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            //Single reason
            if (!IsSingleFlag(dto.Reasons))
                throw new InvalidOperationException("Only one reason may be selected per report.");

            //Prevent self-reporting
            if (dto.Type == ReportType.User && dto.TargetId == userId)
                throw new InvalidOperationException("You cannot report yourself.");

            //1-hour cooldown to prevent spam report
            var lastReport = await _reportRepository.GetLastReportTimeByUserAsync(userId);
            if (lastReport.HasValue && DateTime.UtcNow - lastReport.Value < TimeSpan.FromMinutes(ReportCooldownMinutes))
            {
                var minutesLeft = (int)(ReportCooldownMinutes - (DateTime.UtcNow - lastReport.Value).TotalMinutes) + 1;
                throw new InvalidOperationException($"You must wait {minutesLeft} minute(s) before submitting another report.");
            }

            //Prevent duplicate report on the same target
            if (await _reportRepository.HasReportedTargetAsync(userId, dto.TargetId, dto.Type))
                throw new InvalidOperationException("You have already reported this.");

            var report = new Report
            {
                ReportedById = userId,
                Type = dto.Type,
                TargetId = dto.TargetId,
                Reasons = dto.Reasons,
                AdditionalDetails = dto.AdditionalDetails?.Trim(),
                Status = ReportStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _reportRepository.AddAsync(report);
            await _reportRepository.SaveChangesAsync();

            var created = await _reportRepository.GetByIdWithDetailsAsync(report.Id);
            return MapToDto(created!, userId);
        }

        public async Task<PagedResult<ReportDto>> GetMyReportsAsync(
            string userId,
            ReportFilter? filter,
            PagedRequest request)
        {
            var paged = await _reportRepository.GetByUserIdAsync(userId, filter, request);
            return MapPagedResult(paged, userId);
        }

        public async Task<ReportDto> GetByIdAsync(int id, string userId, bool isAdmin)
        {
            var report = await _reportRepository.GetByIdWithDetailsAsync(id)
                ?? throw new KeyNotFoundException($"Report {id} not found.");

            if (!isAdmin && report.ReportedById != userId)
                throw new UnauthorizedAccessException("You cannot view this report.");

            return MapToDto(report, userId);
        }


        //Admin actions
        public async Task<PagedResult<ReportDto>> GetAllAsync(ReportFilter? filter, PagedRequest request)
        {
            var paged = await _reportRepository.GetAllAsync(filter, request);
            return MapPagedResult(paged, adminView: true);
        }

        public async Task<ReportDto> ResolveReportAsync(int id, string adminId, AdminResolveReportDto dto)
        {
            var report = await _reportRepository.GetByIdWithDetailsAsync(id)
                ?? throw new KeyNotFoundException($"Report {id} not found.");

            var admin = await _userRepository.GetByIdAsync(adminId)
                ?? throw new KeyNotFoundException("Admin not found.");

            if (report.Status == ReportStatus.Resolved || report.Status == ReportStatus.Dismissed)
                throw new InvalidOperationException("This report has already been resolved.");

            if (dto.Status != ReportStatus.Resolved && dto.Status != ReportStatus.Dismissed)
                throw new InvalidOperationException("Resolution status must be Resolved or Dismissed.");

            report.Status = dto.Status;
            report.HandledByAdminId = adminId;
            report.HandledByAdmin = admin;
            report.AdminNote = dto.AdminNote?.Trim();
            report.ResolvedAt = DateTime.UtcNow;

            _reportRepository.Update(report);
            await _reportRepository.SaveChangesAsync();

            return MapToDto(report, adminView: true);
        }

        //Helpers

        //Enforces single reason rule
        private static bool IsSingleFlag(ReportReason reasons)
        {
            return Enum.IsDefined(typeof(ReportReason), reasons);
        }

        private static ReportDto MapToDto(Report report, string? currentUserId = null, bool adminView = false)
        {
            return new ReportDto
            {
                Id = report.Id,
                ReportedById = report.ReportedById,
                ReportedByName = report.ReportedBy?.FullName ?? string.Empty,
                ReportedByUserName = report.ReportedBy?.UserName ?? string.Empty,
                ReportedByAvatarUrl = report.ReportedBy?.AvatarUrl,
                IsMine = currentUserId != null && report.ReportedById == currentUserId,
                Type = report.Type,
                TargetId = report.TargetId,
                Reasons = report.Reasons,
                AdditionalDetails = report.AdditionalDetails,
                Status = report.Status,
                HandledByAdminId = report.HandledByAdminId,
                HandledByAdminName = report.HandledByAdmin?.FullName,
                HandledByAdminUserName = report.HandledByAdmin?.UserName,
                HandledByAdminAvatarUrl = report.HandledByAdmin?.AvatarUrl,
                AdminNote = report.AdminNote,
                ResolvedAt = report.ResolvedAt,
                CreatedAt = report.CreatedAt
            };
        }

        private static PagedResult<ReportDto> MapPagedResult(PagedResult<Report> source, string? currentUserId = null, bool adminView = false)
        {
            return new PagedResult<ReportDto>
            {
                Items = source.Items.Select(r => MapToDto(r, currentUserId, adminView)).ToList(),
                TotalCount = source.TotalCount,
                Page = source.Page,
                PageSize = source.PageSize
            };
        }
    }
}