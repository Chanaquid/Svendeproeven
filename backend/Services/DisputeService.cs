using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Identity;

namespace backend.Services
{
    public class DisputeService : IDisputeService
    {
        private readonly IDisputeRepository _disputeRepository;
        private readonly ILoanRepository _loanRepository;
        private readonly IUserRepository _userRepository;
        private readonly IFineRepository _fineRepository;
        private readonly IScoreHistoryRepository _scoreHistoryRepository;
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public DisputeService(
            IDisputeRepository disputeRepository,
            ILoanRepository loanRepository,
            IUserRepository userRepository,
            IFineRepository fineRepository,
            IScoreHistoryRepository scoreHistoryRepository,
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager)
        {
            _disputeRepository = disputeRepository;
            _loanRepository = loanRepository;
            _userRepository = userRepository;
            _fineRepository = fineRepository;
            _scoreHistoryRepository = scoreHistoryRepository;
            _notificationService = notificationService;
            _userManager = userManager;
        }

        public async Task<DisputeDto> CreateDisputeAsync(string userId, CreateDisputeDto dto)
        {
            var loan = await _loanRepository.GetByIdWithDetailsAsync(dto.LoanId);
            if (loan == null)
                throw new KeyNotFoundException("Loan not found.");

            //Disputes are only allowed once the loan is marked as completed
            if (loan.Status != LoanStatus.Completed)
                throw new InvalidOperationException("Disputes can only be filed for completed loans.");

            if (loan.BorrowerId != userId && loan.LenderId != userId)
                throw new UnauthorizedAccessException("You are not a participant in this loan.");

            var canFile = await CanUserFileDisputeAsync(userId, dto.LoanId);
            if (!canFile)
                throw new InvalidOperationException("You cannot file a dispute for this loan.");

            //Initialize dispute with a 72-hour window for the otherparty to respond
            var dispute = new Dispute
            {
                LoanId = dto.LoanId,
                FiledById = userId,
                FiledAs = dto.FiledAs,
                Description = dto.Description,
                Status = DisputeStatus.AwaitingResponse,
                ResponseDeadline = DateTime.UtcNow.AddHours(72),
                CreatedAt = DateTime.UtcNow,
                IsViewedByOtherParty = false
            };

            await _disputeRepository.AddAsync(dispute);
            await _disputeRepository.SaveChangesAsync();

            //Notify the other participant immediately
            var otherUserId = loan.BorrowerId == userId ? loan.LenderId : loan.BorrowerId;
            await _notificationService.SendAsync(
                otherUserId,
                NotificationType.DisputeFiled,
                $"A dispute has been filed for '{loan.Item.Title}'. You have 72 hours to respond.",
                dispute.Id,
                NotificationReferenceType.Dispute);

            return await GetDisputeByIdAsync(dispute.Id, userId);
        }

        public async Task<DisputeDto> EditDisputeAsync(string userId, int disputeId, EditDisputeDto dto)
        {
            var dispute = await _disputeRepository.GetByIdWithDetailsAsync(disputeId);
            if (dispute == null)
                throw new KeyNotFoundException("Dispute not found.");

            if (dispute.FiledById != userId)
                throw new UnauthorizedAccessException("Only the filer can edit this dispute.");

            //prevent edits once the other party has viewed it
            if (!await IsEditableAsync(disputeId))
                throw new InvalidOperationException("This dispute can no longer be edited because the other party has viewed it.");

            dispute.Description = dto.Description;
            _disputeRepository.Update(dispute);
            await _disputeRepository.SaveChangesAsync();

            return await GetDisputeByIdAsync(disputeId, userId);
        }

        public async Task CancelDisputeAsync(string userId, int disputeId)
        {
            var dispute = await _disputeRepository.GetByIdAsync(disputeId);
            if (dispute == null)
                throw new KeyNotFoundException("Dispute not found.");

            if (dispute.FiledById != userId)
                throw new UnauthorizedAccessException("Only the filer can cancel this dispute.");

            //Cannot cancel if the case has already progressed to Admin Review or a response exists
            if (dispute.Status != DisputeStatus.AwaitingResponse)
                throw new InvalidOperationException("This dispute cannot be cancelled.");

            if (dispute.RespondedById != null)
                throw new InvalidOperationException("Cannot cancel after the other party has responded.");

            dispute.Status = DisputeStatus.Cancelled;
            _disputeRepository.Update(dispute);
            await _disputeRepository.SaveChangesAsync();
        }

        public async Task<DisputePhotoDto> AddFiledByPhotoUrlAsync(string userId, int disputeId, string photoUrl)
        {
            var dispute = await _disputeRepository.GetByIdAsync(disputeId);
            if (dispute == null)
                throw new KeyNotFoundException("Dispute not found.");

            if (dispute.FiledById != userId)
                throw new UnauthorizedAccessException("Only the filer can add photos.");

            if (!await IsEditableAsync(disputeId))
                throw new InvalidOperationException("Photos can no longer be added — the other party has viewed this dispute.");

            var photo = new DisputePhoto
            {
                DisputeId = disputeId,
                SubmittedById = userId,
                PhotoUrl = photoUrl,
                UploadedAt = DateTime.UtcNow
            };

            await _disputeRepository.AddPhotoAsync(photo);
            await _disputeRepository.SaveChangesAsync();

            var user = await _userManager.FindByIdAsync(userId);

            return new DisputePhotoDto
            {
                Id = photo.Id,
                DisputeId = photo.DisputeId,
                PhotoUrl = photo.PhotoUrl,
                SubmittedById = photo.SubmittedById,
                SubmittedByName = user?.FullName ?? "Unknown",
                SubmittedByUserName = user?.UserName ?? "Unknown",
                SubmittedByAvatarUrl = user?.AvatarUrl,
                UploadedAt = photo.UploadedAt
            };
        }

        public async Task DeleteFiledByPhotoAsync(string userId, int disputeId, int photoId)
        {
            var dispute = await _disputeRepository.GetByIdAsync(disputeId);
            if (dispute == null)
                throw new KeyNotFoundException("Dispute not found.");

            if (dispute.FiledById != userId)
                throw new UnauthorizedAccessException("Only the filer can delete photos.");

            if (!await IsEditableAsync(disputeId))
                throw new InvalidOperationException("Photos can no longer be deleted — the other party has viewed this dispute.");

            var photo = await _disputeRepository.GetPhotoByIdAsync(photoId);
            if (photo == null || photo.DisputeId != disputeId)
                throw new KeyNotFoundException("Photo not found.");

            await _disputeRepository.DeletePhotoAsync(photo);
            await _disputeRepository.SaveChangesAsync();
        }

        public async Task MarkViewedByOtherPartyAsync(string userId, int disputeId)
        {
            var dispute = await _disputeRepository.GetByIdAsync(disputeId);
            if (dispute == null)
                throw new KeyNotFoundException("Dispute not found.");

            //Only mark as viewed if the person viewing is the Respondent, not the Filer
            if (dispute.FiledById == userId)
                return;

            var loan = await _loanRepository.GetByIdAsync(dispute.LoanId);
            if (loan == null)
                throw new KeyNotFoundException("Loan not found.");

            if (loan.BorrowerId != userId && loan.LenderId != userId)
                throw new UnauthorizedAccessException("You are not a participant in this dispute.");

            if (dispute.IsViewedByOtherParty)
                return;

            dispute.IsViewedByOtherParty = true;
            dispute.FirstViewedByOtherPartyAt = DateTime.UtcNow;

            _disputeRepository.Update(dispute);
            await _disputeRepository.SaveChangesAsync();
        }

        public async Task<DisputeDto> SubmitResponseAsync(string userId, int disputeId, SubmitDisputeResponseDto dto)
        {
            var dispute = await _disputeRepository.GetByIdWithDetailsAsync(disputeId);
            if (dispute == null)
                throw new KeyNotFoundException("Dispute not found.");

            if (dispute.FiledById == userId)
                throw new InvalidOperationException("You cannot respond to your own dispute.");

            var loan = dispute.Loan;
            if (loan.BorrowerId != userId && loan.LenderId != userId)
                throw new UnauthorizedAccessException("You are not a participant in this dispute.");

            if (dispute.RespondedById != null)
                throw new InvalidOperationException("You have already responded to this dispute.");

            if (dispute.Status != DisputeStatus.AwaitingResponse)
                throw new InvalidOperationException("This dispute is no longer accepting responses.");

            if (DateTime.UtcNow > dispute.ResponseDeadline)
                throw new InvalidOperationException("The response deadline has passed.");


            dispute.RespondedById = userId;
            dispute.ResponseDescription = dto.ResponseDescription;
            dispute.RespondedAt = DateTime.UtcNow;
            dispute.Status = DisputeStatus.PendingAdminReview;

            _disputeRepository.Update(dispute);
            await _disputeRepository.SaveChangesAsync();

            await _notificationService.SendToAdminsAsync(
                NotificationType.DisputeResponseSubmitted,
                $"Response submitted for dispute #{disputeId} — '{loan.Item.Title}'.",
                disputeId,
                NotificationReferenceType.Dispute);

            return await GetDisputeByIdAsync(disputeId, userId);
        }

        public async Task<DisputePhotoDto> AddResponsePhotoUrlAsync(string userId, int disputeId, string photoUrl)
        {
            var dispute = await _disputeRepository.GetByIdAsync(disputeId);
            if (dispute == null)
                throw new KeyNotFoundException("Dispute not found.");

            if (dispute.FiledById == userId)
                throw new UnauthorizedAccessException("Only the respondent can add response photos.");

            if (dispute.Status != DisputeStatus.AwaitingResponse)
                throw new InvalidOperationException("Response photos can only be added while the dispute is awaiting response.");

            if (DateTime.UtcNow > dispute.ResponseDeadline)
                throw new InvalidOperationException("The response deadline has passed.");

            var photo = new DisputePhoto
            {
                DisputeId = disputeId,
                SubmittedById = userId,
                PhotoUrl = photoUrl,
                UploadedAt = DateTime.UtcNow
            };

            await _disputeRepository.AddPhotoAsync(photo);
            await _disputeRepository.SaveChangesAsync();

            var user = await _userManager.FindByIdAsync(userId);

            return new DisputePhotoDto
            {
                Id = photo.Id,
                DisputeId = photo.DisputeId,
                PhotoUrl = photo.PhotoUrl,
                SubmittedById = photo.SubmittedById,
                SubmittedByName = user?.FullName ?? "Unknown",
                SubmittedByUserName = user?.UserName ?? "Unknown",
                SubmittedByAvatarUrl = user?.AvatarUrl,
                UploadedAt = photo.UploadedAt
            };
        }

        public async Task<DisputeDto> ResolveDisputeAsync(string adminId, int disputeId, AdminResolveDisputeDto dto)
        {
            var dispute = await _disputeRepository.GetByIdWithDetailsAsync(disputeId);
            if (dispute == null)
                throw new KeyNotFoundException("Dispute not found.");

            if (dispute.Status == DisputeStatus.Resolved)
                throw new InvalidOperationException("This dispute has already been resolved.");

            if (dispute.Status != DisputeStatus.PendingAdminReview && dispute.Status != DisputeStatus.PastDeadline)
                throw new InvalidOperationException("Only disputes under review or past deadline can be resolved.");

            var admin = await _userManager.FindByIdAsync(adminId);
            if (admin == null)
                throw new KeyNotFoundException("Admin not found.");

            await ApplyPenaltiesAsync(dispute, dto, adminId);

            dispute.AdminVerdict = dto.Verdict;
            dispute.AdminNote = dto.AdminNote;
            dispute.ResolvedByAdminId = adminId;
            dispute.ResolvedByAdmin = admin;
            dispute.ResolvedAt = DateTime.UtcNow;
            dispute.Status = DisputeStatus.Resolved;

            _disputeRepository.Update(dispute);
            await _disputeRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                dispute.FiledById,
                NotificationType.DisputeResolved,
                $"The dispute for '{dispute.Loan.Item.Title}' has been resolved. Verdict: {dto.Verdict}",
                disputeId,
                NotificationReferenceType.Dispute);

            var otherPartyId = dispute.RespondedById
                ?? (dispute.Loan.BorrowerId == dispute.FiledById
                    ? dispute.Loan.LenderId
                    : dispute.Loan.BorrowerId);

            await _notificationService.SendAsync(
                otherPartyId,
                NotificationType.DisputeResolved,
                $"The dispute for '{dispute.Loan.Item.Title}' has been resolved. Verdict: {dto.Verdict}",
                disputeId,
                NotificationReferenceType.Dispute);

            return await GetDisputeByIdAsync(disputeId, dispute.FiledById);
        }

        public async Task<DisputeDto> GetDisputeByIdAsync(int disputeId, string userId)
        {
            var dispute = await _disputeRepository.GetByIdWithDetailsAsync(disputeId);
            if (dispute == null)
                throw new KeyNotFoundException("Dispute not found.");

            if (!await IsUserPartyToDisputeAsync(userId, disputeId))
                throw new UnauthorizedAccessException("You do not have access to this dispute.");

            return MapToDisputeDto(dispute, userId);
        }

        public async Task<PagedResult<DisputeListDto>> GetFiledByUserAsync(
            string userId, DisputeFilter? filter, PagedRequest request)
        {
            var result = await _disputeRepository.GetFiledByUserAsync(userId, filter, request);
            return new PagedResult<DisputeListDto>
            {
                Items = result.Items.Select(d => MapToDisputeListDto(d, userId)).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<PagedResult<DisputeListDto>> GetRespondedToByUserAsync(
            string userId, DisputeFilter? filter, PagedRequest request)
        {
            var result = await _disputeRepository.GetRespondedToByUserAsync(userId, filter, request);
            return new PagedResult<DisputeListDto>
            {
                Items = result.Items.Select(d => MapToDisputeListDto(d, userId)).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<PagedResult<DisputeListDto>> GetAllDisputesByUserIdAsync(
            string userId, DisputeFilter? filter, PagedRequest request)
        {
            var result = await _disputeRepository.GetAllDisputesByUserIdAsync(userId, filter, request);
            return new PagedResult<DisputeListDto>
            {
                Items = result.Items.Select(d => MapToDisputeListDto(d, userId)).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<DisputeDto> AdminGetDisputeByIdAsync(int disputeId)
        {
            var dispute = await _disputeRepository.GetByIdWithDetailsAsync(disputeId);
            if (dispute == null)
                throw new KeyNotFoundException("Dispute not found.");

            return MapToDisputeDto(dispute, null);
        }

        public async Task<PagedResult<DisputeListDto>> GetAllDisputesAsync(
            DisputeFilter? filter, PagedRequest request)
        {
            var result = await _disputeRepository.GetAllAsync(filter, request);
            return new PagedResult<DisputeListDto>
            {
                Items = result.Items.Select(d => MapToDisputeListDto(d, null)).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<PagedResult<DisputeListDto>> GetAllOpenDisputesAsync(
            DisputeFilter? filter, PagedRequest request)
        {
            var result = await _disputeRepository.GetAllOpenAsync(filter, request);
            return new PagedResult<DisputeListDto>
            {
                Items = result.Items.Select(d => MapToDisputeListDto(d, null)).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<PagedResult<DisputeListDto>> GetDisputesByStatusAsync(
            DisputeStatus status, DisputeFilter? filter, PagedRequest request)
        {
            var result = await _disputeRepository.GetByStatusAsync(status, filter, request);
            return new PagedResult<DisputeListDto>
            {
                Items = result.Items.Select(d => MapToDisputeListDto(d, null)).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<List<DisputeDto>> GetDisputesByLoanIdAsync(int loanId)
        {
            var disputes = await _disputeRepository.GetByLoanIdAsync(loanId);
            return disputes.Select(d => MapToDisputeDto(d, null)).ToList();
        }

        public async Task<PagedResult<DisputeListDto>> GetDisputeHistoryByItemAsync(
            int itemId, DisputeFilter? filter, PagedRequest request)
        {
            var result = await _disputeRepository.GetDisputeHistoryByItemIdAsync(itemId, filter, request);
            return new PagedResult<DisputeListDto>
            {
                Items = result.Items.Select(d => MapToDisputeListDto(d, null)).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<DisputeStatsDto> GetDisputeStatsAsync()
        {
            var statusCounts = await _disputeRepository.GetStatusCountsAsync();
            var verdictCounts = await _disputeRepository.GetVerdictCountsAsync();
            var now = DateTime.UtcNow;

            return new DisputeStatsDto
            {
                TotalOpen = await _disputeRepository.GetOpenCountAsync(),
                AwaitingResponse = statusCounts.GetValueOrDefault(DisputeStatus.AwaitingResponse, 0),
                UnderReview = statusCounts.GetValueOrDefault(DisputeStatus.PendingAdminReview, 0),
                OverdueResponse = await _disputeRepository.GetPastDeadlineCountAsync(),
                ResolvedThisMonth = await _disputeRepository.GetResolvedCountByMonthAsync(now.Year, now.Month),
                VerdictBreakdown = verdictCounts
            };
        }

        public async Task<bool> CanUserFileDisputeAsync(string userId, int loanId)
        {
            var loan = await _loanRepository.GetByIdAsync(loanId);
            if (loan == null) return false;
            if (loan.BorrowerId != userId && loan.LenderId != userId) return false;
            if (loan.Status != LoanStatus.Completed) return false;

            //Check 14-day dispute window
            if (loan.DisputeDeadline.HasValue && DateTime.UtcNow > loan.DisputeDeadline.Value)
                return false;

            //Prevent duplicate or overlapping disputes for the same loan
            if (await _disputeRepository.HasUserFiledDisputeForLoanAsync(loanId, userId)) return false;
            if (await _disputeRepository.HasActiveDisputeAsync(loanId)) return false;

            //Max 2 disputes total per loan (one from each party)
            var disputeCount = await _disputeRepository.GetDisputeCountForLoanAsync(loanId);
            if (disputeCount >= 2) return false;

            return true;
        }

        public async Task<bool> IsUserPartyToDisputeAsync(string userId, int disputeId)
        {
            var dispute = await _disputeRepository.GetByIdAsync(disputeId);
            if (dispute == null) return false;

            if (dispute.FiledById == userId || dispute.RespondedById == userId)
                return true;

            var loan = await _loanRepository.GetByIdAsync(dispute.LoanId);
            if (loan == null) return false;

            return loan.BorrowerId == userId || loan.LenderId == userId;
        }

        public async Task<bool> IsEditableAsync(int disputeId)
        {
            var dispute = await _disputeRepository.GetByIdAsync(disputeId);
            if (dispute == null) return false;

            return !dispute.IsViewedByOtherParty &&
                   dispute.Status == DisputeStatus.AwaitingResponse;
        }


        public async Task<int> ProcessExpiredDisputesAsync()
        {
            var expiredDisputes = await _disputeRepository.GetExpiredAwaitingResponseAsync();
            int count = 0;

            foreach (var dispute in expiredDisputes)
            {
                dispute.Status = DisputeStatus.PastDeadline;
                _disputeRepository.Update(dispute);
                count++;
            }

            if (count > 0)
                await _disputeRepository.SaveChangesAsync();

            return count;
        }


        //Helpers
        private async Task ApplyPenaltiesAsync(Dispute dispute, AdminResolveDisputeDto dto, string adminId)
        {
            var loan = dispute.Loan;
            var owner = await _userManager.FindByIdAsync(loan.LenderId);
            var borrower = await _userManager.FindByIdAsync(loan.BorrowerId);

            if (dto.OwnerPenalty != null && owner != null)
                await ApplyPenaltyToUserAsync(owner, dto.OwnerPenalty, dispute, adminId);

            if (dto.BorrowerPenalty != null && borrower != null)
                await ApplyPenaltyToUserAsync(borrower, dto.BorrowerPenalty, dispute, adminId);
        }

        private async Task ApplyPenaltyToUserAsync(
            ApplicationUser user,
            DisputePenaltyDto penalty,
            Dispute dispute,
            string adminId)
        {
            if (penalty.FineAmount.HasValue && penalty.FineAmount.Value > 0)
            {
                var fine = new Fine
                {
                    UserId = user.Id,
                    Amount = penalty.FineAmount.Value,
                    Type = FineType.ResultedByDispute,
                    Status = FineStatus.Unpaid,
                    CreatedAt = DateTime.UtcNow,
                    DisputeId = dispute.Id,
                    LoanId = dispute.LoanId,
                    IssuedByAdminId = adminId
                };
                await _fineRepository.AddAsync(fine);
            }

            //Adjust trust score (0-100) and log the change for transparency
            if (penalty.ScoreAdjustment.HasValue && penalty.ScoreAdjustment.Value != 0)
            {
                var newScore = Math.Clamp(user.Score + penalty.ScoreAdjustment.Value, 0, 100);

                await _scoreHistoryRepository.AddAsync(new ScoreHistory
                {
                    UserId = user.Id,
                    PointsChanged = penalty.ScoreAdjustment.Value,
                    ScoreAfterChange = newScore,
                    Reason = ScoreChangeReason.DisputePenalty,
                    LoanId = dispute.LoanId,
                    DisputeId = dispute.Id,
                    Note = $"Dispute #{dispute.Id} penalty.",
                    CreatedAt = DateTime.UtcNow
                });

                user.Score = newScore;
                _userRepository.Update(user);
            }
        }

        private DisputeDto MapToDisputeDto(Dispute dispute, string? currentUserId)
        {
            //Complex mapping that includes condition snapshots and filtered photo sets
            var isMine = currentUserId != null && dispute.FiledById == currentUserId;

            return new DisputeDto
            {
                Id = dispute.Id,
                LoanId = dispute.LoanId,
                ItemId = dispute.Loan.ItemId,
                ItemTitle = dispute.Loan.Item.Title,
                IsMine = isMine,
                FiledById = dispute.FiledById,
                FiledByName = dispute.FiledBy?.FullName ?? "Unknown",
                FiledByUserName = dispute.FiledBy?.UserName ?? "Unknown",
                FiledByAvatarUrl = dispute.FiledBy?.AvatarUrl,
                FiledAs = dispute.FiledAs,
                Description = dispute.Description,
                FiledByPhotos = dispute.Photos
                    .Where(p => p.SubmittedById == dispute.FiledById)
                    .Select(p => new DisputePhotoDto
                    {
                        Id = p.Id,
                        DisputeId = p.DisputeId,
                        PhotoUrl = p.PhotoUrl,
                        SubmittedById = p.SubmittedById,
                        SubmittedByName = dispute.FiledBy?.FullName ?? "Unknown",
                        SubmittedByUserName = dispute.FiledBy?.UserName ?? "Unknown",
                        SubmittedByAvatarUrl = dispute.FiledBy?.AvatarUrl,
                        IsMine = p.SubmittedById == currentUserId,
                        UploadedAt = p.UploadedAt
                    }).ToList(),
                RespondedById = dispute.RespondedById,
                RespondedByName = dispute.RespondedBy?.FullName,
                RespondedByUserName = dispute.RespondedBy?.UserName,
                RespondedByAvatarUrl = dispute.RespondedBy?.AvatarUrl,
                ResponseDescription = dispute.ResponseDescription,
                RespondedAt = dispute.RespondedAt,
                ResponseDeadline = dispute.ResponseDeadline,
                ResponsePhotos = dispute.Photos
                    .Where(p => p.SubmittedById == dispute.RespondedById)
                    .Select(p => new DisputePhotoDto
                    {
                        Id = p.Id,
                        DisputeId = p.DisputeId,
                        PhotoUrl = p.PhotoUrl,
                        SubmittedById = p.SubmittedById,
                        SubmittedByName = dispute.RespondedBy?.FullName ?? "Unknown",
                        SubmittedByUserName = dispute.RespondedBy?.UserName ?? "Unknown",
                        SubmittedByAvatarUrl = dispute.RespondedBy?.AvatarUrl,
                        IsMine = p.SubmittedById == currentUserId,
                        UploadedAt = p.UploadedAt
                    }).ToList(),
                Status = dispute.Status,
                CreatedAt = dispute.CreatedAt,
                ResolvedAt = dispute.ResolvedAt,
                AdminVerdict = dispute.AdminVerdict,
                AdminNote = dispute.AdminNote,
                ResolvedByAdminId = dispute.ResolvedByAdminId,
                ResolvedByAdminName = dispute.ResolvedByAdmin?.FullName,
                ResolvedByAdminUserName = dispute.ResolvedByAdmin?.UserName,
                ResolvedByAdminAvatarUrl = dispute.ResolvedByAdmin?.AvatarUrl,
                SnapshotCondition = dispute.Loan.SnapshotCondition,
                SnapshotPhotos = dispute.Loan.SnapshotPhotos?.Select(sp => new LoanSnapshotPhotoDto
                {
                    Id = sp.Id,
                    LoanId = sp.LoanId,
                    PhotoUrl = sp.PhotoUrl,
                    DisplayOrder = sp.DisplayOrder,
                    SnapshotTakenAt = sp.SnapshotTakenAt
                }).ToList() ?? new(),
                Penalties = dispute.Fines.Select(f => new DisputePenaltySummaryDto
                {
                    UserId = f.UserId,
                    FullName = f.UserId == dispute.Loan.LenderId
                        ? (dispute.Loan.Lender?.FullName ?? "Unknown")
                        : (dispute.Loan.Borrower?.FullName ?? "Unknown"),
                    UserName = f.UserId == dispute.Loan.LenderId
                        ? (dispute.Loan.Lender?.UserName ?? "Unknown")
                        : (dispute.Loan.Borrower?.UserName ?? "Unknown"),
                    AvatarUrl = f.UserId == dispute.Loan.LenderId
                        ? dispute.Loan.Lender?.AvatarUrl
                        : dispute.Loan.Borrower?.AvatarUrl,
                    FineAmount = f.Amount,
                    FineStatus = f.Status.ToString(),
                    ScoreAdjustment = null
                }).ToList()
            };
        }

        private DisputeListDto MapToDisputeListDto(Dispute dispute, string? currentUserId)
        {
            var isFiler = currentUserId != null && dispute.FiledById == currentUserId;

            var otherParty = isFiler
                ? (dispute.Loan.BorrowerId == currentUserId ? dispute.Loan.Lender : dispute.Loan.Borrower)
                : dispute.FiledBy;

            return new DisputeListDto
            {
                Id = dispute.Id,
                LoanId = dispute.LoanId,
                ItemTitle = dispute.Loan.Item.Title,
                FiledById = dispute.FiledById,
                FiledByName = dispute.FiledBy?.FullName ?? "Unknown",
                FiledByUsername = dispute.FiledBy?.UserName ?? "Unknown",
                FiledByAvatarUrl = dispute.FiledBy?.AvatarUrl,
                FiledAs = dispute.FiledAs,
                OtherPartyName = otherParty?.FullName,
                OtherPartyUserName = otherParty?.UserName,
                OtherPartyAvatarUrl = otherParty?.AvatarUrl,
                Status = dispute.Status,
                HasResponse = dispute.RespondedById != null,
                IsOverDue = dispute.Status == DisputeStatus.PastDeadline,
                ResponseDeadline = dispute.ResponseDeadline,
                CreatedAt = dispute.CreatedAt
            };
        }
    }
}