using backend.Dtos;
using backend.Interfaces;
using backend.Models;

namespace backend.Services
{
    public class AppealService : IAppealService
    {
        private readonly IAppealRepository _appealRepository;
        private readonly IUserRepository _userRepository;
        private readonly IFineRepository _fineRepository;
        private readonly IScoreHistoryRepository _scoreHistoryRepository;
        private readonly INotificationService _notificationService;

        public AppealService(
            IAppealRepository appealRepository,
            IUserRepository userRepository,
            IFineRepository fineRepository,
            INotificationService notificationService,
            IScoreHistoryRepository scoreHistoryRepository)
        {
            _appealRepository = appealRepository;
            _userRepository = userRepository;
            _fineRepository = fineRepository;
            _notificationService = notificationService;
            _scoreHistoryRepository = scoreHistoryRepository;
        }

        // Score restored on appeal approval — reset to just above the hard block threshold
        private const int DefaultRestoredScore = 20;

        //User actions

        public async Task<AppealDto> CreateScoreAppealAsync(string userId, CreateScoreAppealDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            if (user.Score >= 20)
                throw new InvalidOperationException("Your score is 20 or above. You do not need a score appeal.");

            if (await _appealRepository.HasPendingScoreAppealAsync(userId))
                throw new InvalidOperationException("You already have a pending score appeal.");

            if (user.LastScoreAppealRejectedAt.HasValue && DateTime.UtcNow - user.LastScoreAppealRejectedAt.Value < TimeSpan.FromHours(12))
                throw new InvalidOperationException("You must wait 12 hours before filing another score appeal.");

            if (string.IsNullOrWhiteSpace(dto.Message))
                throw new ArgumentException("Appeal message cannot be empty.");

            var appeal = new Appeal
            {
                UserId = userId,
                Message = dto.Message.Trim(),
                Status = AppealStatus.Pending,
                AppealType = AppealType.Score,
                CreatedAt = DateTime.UtcNow
            };

            await _appealRepository.AddAsync(appeal);
            await _appealRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                userId,
                NotificationType.AppealSubmitted,
                "Your score appeal has been submitted and will be reviewed by an admin shortly.",
                appeal.Id,
                NotificationReferenceType.Appeal
            );

            //Re-fetch with details so User navigation property is populated for mapping
            var created = await _appealRepository.GetByIdWithDetailsAsync(appeal.Id);
            return MapToAppealDto(created!);
        }

        public async Task<AppealDto> CreateFineAppealAsync(string userId, CreateFineAppealDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            if (string.IsNullOrWhiteSpace(dto.Message))
                throw new ArgumentException("Appeal message cannot be empty.");

            var fine = await _fineRepository.GetByIdAsync(dto.FineId)
                ?? throw new KeyNotFoundException("Fine not found.");

            if (fine.UserId != userId)
                throw new UnauthorizedAccessException("You can only appeal your own fines.");

            if (fine.Status == FineStatus.Paid || fine.Status == FineStatus.Voided)
                throw new InvalidOperationException("This fine is already closed and cannot be appealed.");

            if (await _appealRepository.HasFineAppealAsync(user.Id, dto.FineId))
                throw new InvalidOperationException("An appeal has already been filed for this fine and cannot be refiled.");

            var appeal = new Appeal
            {
                UserId = userId,
                FineId = dto.FineId,
                Message = dto.Message.Trim(),
                Status = AppealStatus.Pending,
                AppealType = AppealType.Fine,
                CreatedAt = DateTime.UtcNow
            };

            await _appealRepository.AddAsync(appeal);
            await _appealRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                userId,
                NotificationType.AppealSubmitted,
                "Your fine appeal has been submitted and will be reviewed by an admin shortly.",
                appeal.Id,
                NotificationReferenceType.Appeal
            );

            var created = await _appealRepository.GetByIdWithDetailsAsync(appeal.Id);
            return MapToAppealDto(created!);
        }

        public async Task<AppealDto> GetByIdAsync(int appealId, string userId, bool isAdmin = false)
        {
            var appeal = await _appealRepository.GetByIdWithDetailsAsync(appealId)
                ?? throw new KeyNotFoundException($"Appeal {appealId} not found.");

            if (!isAdmin && appeal.UserId != userId)
                throw new UnauthorizedAccessException("You cannot view this appeal.");

            return MapToAppealDto(appeal);
        }

        public async Task<PagedResult<AppealDto>> GetMyAppealsAsync(
            string userId,
            AppealFilter? filter,
            PagedRequest request)
        {

            var pagedAppeals = await _appealRepository
                .GetAllByUserIdAsync(userId, filter, request);

            return MapPagedResult(pagedAppeals);
        }

        public async Task CancelAppealAsync(int appealId, string userId)
        {
            var appeal = await _appealRepository.GetByIdAsync(appealId)
                ?? throw new KeyNotFoundException($"Appeal {appealId} not found.");

            if (appeal.UserId != userId)
                throw new UnauthorizedAccessException("Only the creator can cancel this appeal.");

            if (appeal.Status != AppealStatus.Pending)
                throw new InvalidOperationException("Only pending appeals can be cancelled.");

            appeal.Status = AppealStatus.Cancelled;
            _appealRepository.Update(appeal);
            await _appealRepository.SaveChangesAsync();
        }

        public async Task DeleteAppealAsync(int appealId, string userId)
        {
            var appeal = await _appealRepository.GetByIdAsync(appealId)
                ?? throw new KeyNotFoundException($"Appeal {appealId} not found.");

            if (appeal.UserId != userId)
                throw new UnauthorizedAccessException("You can only delete your own appeals.");

            if (appeal.Status != AppealStatus.Pending && appeal.Status != AppealStatus.Cancelled)
                throw new InvalidOperationException("You can only delete pending or cancelled appeals.");



            appeal.IsDeleted = true;
            appeal.DeletedAt = DateTime.UtcNow;
            appeal.Status = AppealStatus.Deleted;
            _appealRepository.Update(appeal);
            await _appealRepository.SaveChangesAsync();
        }



        //admin actions
        public async Task<PagedResult<AppealDto>> GetAllAppealsByUserIdAsync(
            string userId,
            AppealFilter? filter,
            PagedRequest request)
        {

            var pagedAppeals = await _appealRepository.GetAllByUserIdAsync(userId, filter, request);
            return MapPagedResult(pagedAppeals);
        }

        public async Task<AppealDto> GetByIdWithDetailsAsync(int appealId)
        {
            var appeal = await _appealRepository.GetByIdWithDetailsAsync(appealId)
                ?? throw new KeyNotFoundException($"Appeal {appealId} not found.");

            return MapToAppealDto(appeal);
        }

        public async Task<PagedResult<AppealDto>> GetAllAppealsAsync(AppealFilter? filter, PagedRequest request)
        {
            var pagedAppeals = await _appealRepository.GetAllAsync(filter, request);
            return MapPagedResult(pagedAppeals);
        }

        public async Task<PagedResult<AppealDto>> GetAllPendingAsync(AppealFilter? filter, PagedRequest request)
        {
            var pagedAppeals = await _appealRepository.GetAllByStatusAsync(AppealStatus.Pending, filter, request);
            return MapPagedResult(pagedAppeals);
        }

        public async Task<PagedResult<AppealDto>> GetAllByStatusAsync(AppealStatus status, AppealFilter? filter, PagedRequest request)
        {
            var pagedAppeals = await _appealRepository.GetAllByStatusAsync(status, filter, request);
            return MapPagedResult(pagedAppeals);
        }

        public async Task<AppealDto> DecideScoreAppealAsync(int appealId, string adminId, AdminDecidesScoreAppealDto dto)
        {
            var appeal = await _appealRepository.GetByIdWithDetailsAsync(appealId)
                ?? throw new KeyNotFoundException($"Appeal {appealId} not found.");

            var admin = await _userRepository.GetByIdAsync(adminId)
                ?? throw new KeyNotFoundException("Admin not found.");

            if (appeal.AppealType != AppealType.Score)
                throw new InvalidOperationException("This is not a score appeal.");

            if (appeal.Status != AppealStatus.Pending)
                throw new InvalidOperationException("This appeal has already been resolved.");

            if (!dto.IsApproved && string.IsNullOrWhiteSpace(dto.AdminNote))
                throw new ArgumentException("A reason is required when rejecting an appeal.");

            appeal.Status = dto.IsApproved ? AppealStatus.Approved : AppealStatus.Rejected;
            appeal.ResolvedByAdminId = adminId;
            appeal.ResolvedByAdmin = admin; // Keep navigation property in sync for mapping
            appeal.AdminNote = dto.AdminNote?.Trim();
            appeal.ResolvedAt = DateTime.UtcNow;

            if (dto.IsApproved)
            {
                var targetScore = dto.NewScore ?? DefaultRestoredScore;

                if (targetScore < 1 || targetScore > 100)
                    throw new ArgumentException("Score must be between 1 and 100.");

                var user = appeal.User;
                var pointsChanged = targetScore - user.Score;

                await _scoreHistoryRepository.AddAsync(new ScoreHistory
                {
                    UserId = user.Id,
                    PointsChanged = pointsChanged,
                    ScoreAfterChange = targetScore,
                    Reason = ScoreChangeReason.AppealOutcome,
                    Note = $"Score appeal approved. Score set to {targetScore}.",
                    CreatedAt = DateTime.UtcNow
                });

                user.Score = targetScore;
                _userRepository.Update(user);

                appeal.RestoredScore = targetScore;
            }

            _appealRepository.Update(appeal);
            await _userRepository.SaveChangesAsync();
            await _appealRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                appeal.UserId,
                dto.IsApproved ? NotificationType.AppealApproved : NotificationType.AppealRejected,
                dto.IsApproved
                    ? $"Your score appeal has been approved. Your score has been set to {appeal.RestoredScore}."
                    : $"Your score appeal has been rejected. Reason: {dto.AdminNote}",
                appeal.Id,
                NotificationReferenceType.Appeal
            );

            return MapToAppealDto(appeal);
        }

        public async Task<AppealDto> DecideFineAppealAsync(int appealId, string adminId, AdminDecidesFineAppealDto dto)
        {
            var appeal = await _appealRepository.GetByIdWithDetailsAsync(appealId)
                ?? throw new KeyNotFoundException($"Appeal {appealId} not found.");

            var admin = await _userRepository.GetByIdAsync(adminId)
                ?? throw new KeyNotFoundException("Admin not found.");

            if (appeal.AppealType != AppealType.Fine)
                throw new InvalidOperationException("This is not a fine appeal.");

            if (appeal.Status != AppealStatus.Pending)
                throw new InvalidOperationException("This appeal has already been resolved.");

            if (!dto.IsApproved && string.IsNullOrWhiteSpace(dto.AdminNote))
                throw new ArgumentException("A reason is required when rejecting an appeal.");

            if (dto.IsApproved && dto.Resolution == null)
                throw new ArgumentException("A resolution is required when approving a fine appeal.");

            if (dto.IsApproved && dto.Resolution == FineAppealResolution.Custom
                && (dto.CustomFineAmount == null || dto.CustomFineAmount < 0))
                throw new ArgumentException("A valid custom fine amount is required.");

            appeal.Status = dto.IsApproved ? AppealStatus.Approved : AppealStatus.Rejected;
            appeal.ResolvedByAdminId = adminId;
            appeal.ResolvedByAdmin = admin; //Keep navigation property in sync for mapping
            appeal.AdminNote = dto.AdminNote?.Trim();
            appeal.CustomFineAmount = dto.CustomFineAmount;
            appeal.FineResolution = dto.Resolution;
            appeal.ResolvedAt = DateTime.UtcNow;

            if (dto.IsApproved && appeal.FineId.HasValue)
            {
                var fine = await _fineRepository.GetByIdWithDetailsAsync(appeal.FineId.Value);
                if (fine != null)
                {
                    var originalAmount = fine.Amount;

                    var newAmount = dto.Resolution switch
                    {
                        FineAppealResolution.Voided => 0m,
                        FineAppealResolution.Custom => Math.Round(dto.CustomFineAmount!.Value, 2),
                        _ => fine.Amount
                    };

                    fine.Amount = newAmount;
                    fine.Status = dto.Resolution == FineAppealResolution.Voided
                        ? FineStatus.Voided
                        : FineStatus.Unpaid;

                    if (fine.Status == FineStatus.Voided)
                        fine.VoidAt = DateTime.UtcNow;

                    _fineRepository.Update(fine);
                }
            }

            _appealRepository.Update(appeal);
            await _appealRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                appeal.UserId,
                dto.IsApproved ? NotificationType.AppealApproved : NotificationType.AppealRejected,
                dto.IsApproved
                    ? $"Your fine appeal has been approved. Resolution: {dto.Resolution}."
                    : $"Your fine appeal has been rejected. Reason: {dto.AdminNote}",
                appeal.Id,
                NotificationReferenceType.Appeal
            );

            return MapToAppealDto(appeal);
        }

         //Mappers

        private static AppealDto MapToAppealDto(Appeal appeal)
        {
            return new AppealDto
            {
                Id = appeal.Id,
                UserId = appeal.UserId,
                FullName = appeal.User?.FullName ?? string.Empty,
                UserName = appeal.User?.UserName ?? string.Empty,
                UserAvatarUrl = appeal.User?.AvatarUrl,
                AppealType = appeal.AppealType,
                Message = appeal.Message,
                Status = appeal.Status,

                //Fine-related properties
                FineId = appeal.FineId,
                FineAmount = appeal.Fine?.Amount,
                FineResolution = appeal.FineResolution,
                CustomFineAmount = appeal.CustomFineAmount,

                //Score-related properties 
                ScoreHistoryId = appeal.ScoreHistoryId,
                RestoredScore = appeal.RestoredScore,
                ScoreAfterChange = appeal.AppealType == AppealType.Score
                    ? appeal.ScoreHistory?.ScoreAfterChange ?? appeal.RestoredScore ?? appeal.User?.Score
                    : null,

                // Admin response properties
                AdminNote = appeal.AdminNote,
                ResolvedByAdminId = appeal.ResolvedByAdminId,
                ResolvedByAdminName = appeal.ResolvedByAdmin?.FullName ?? string.Empty,
                ResolvedByAdminUserName = appeal.ResolvedByAdmin?.UserName ?? string.Empty,
                ResolvedByAdminAvatarUrl = appeal.ResolvedByAdmin?.AvatarUrl,

                CreatedAt = appeal.CreatedAt,
                ResolvedAt = appeal.ResolvedAt
            };
        }

        //converts a paged Appeal result to a paged AppealDto result
        private static PagedResult<AppealDto> MapPagedResult(PagedResult<Appeal> source)
        {
            return new PagedResult<AppealDto>
            {
                Items = source.Items.Select(a => MapToAppealDto(a)).ToList(),
                TotalCount = source.TotalCount,
                Page = source.Page,
                PageSize = source.PageSize
            };
        }
    }
}