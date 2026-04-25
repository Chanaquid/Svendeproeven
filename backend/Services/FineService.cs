using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Identity;

namespace backend.Services
{
    public class FineService : IFineService
    {
        private readonly IFineRepository _fineRepository;
        private readonly INotificationService _notificationService;
        private readonly ILoanRepository _loanRepository;
        private readonly IDisputeRepository _disputeRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public FineService(
            IFineRepository fineRepository,
            INotificationService notificationService,
            ILoanRepository loanRepository,
            IDisputeRepository disputeRepository,
            UserManager<ApplicationUser> userManager)
        {
            _fineRepository = fineRepository;
            _notificationService = notificationService;
            _loanRepository = loanRepository;
            _disputeRepository = disputeRepository;
            _userManager = userManager;
        }

        //Issue a fine tied to a loan and dispute
        public async Task<FineDto> CreateLoanDisputeFineAsync(string adminId, CreateLoanDisputeFineDto dto)
        {
            if (dto.Amount <= 0)
                throw new ArgumentException("Fine amount must be greater than zero.");

            if (string.IsNullOrWhiteSpace(dto.AdminNote))
                throw new ArgumentException("A note/reason is required for fines.");

            var loanExists = await _loanRepository.ExistsAsync(dto.LoanId);
            if (!loanExists)
                throw new KeyNotFoundException("Loan not found.");

            var dispute = await _disputeRepository.GetByIdAsync(dto.DisputeId)
                ?? throw new KeyNotFoundException("Dispute not found.");

            if (dispute.LoanId != dto.LoanId)
                throw new InvalidOperationException("Dispute does not belong to this loan.");

            var existingActiveFine = await _fineRepository.ExistsActiveFineAsync(
                dto.UserId, dto.LoanId, dto.DisputeId);

            if (existingActiveFine)
                throw new InvalidOperationException("An active fine already exists for this loan/dispute.");

            var fine = new Fine
            {
                UserId = dto.UserId,
                Amount = dto.Amount,
                Type = FineType.ResultedByDispute, 
                LoanId = dto.LoanId == 0 ? null : dto.LoanId,
                DisputeId = dto.DisputeId == 0 ? null : dto.DisputeId,
                AdminNote = dto.AdminNote,
                Status = FineStatus.Unpaid,
                IssuedByAdminId = adminId,
                CreatedAt = DateTime.UtcNow
            };

            await _fineRepository.AddAsync(fine);
            await _fineRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                dto.UserId,
                NotificationType.FineIssued,
                $"A fine of {dto.Amount:C} has been issued to your account.",
                fine.Id,
                NotificationReferenceType.Fine);

            return await AdminGetFineByIdAsync(fine.Id);
        }

        //Issue a custom fine - No need dispute
        public async Task<FineDto> CreateCustomFineAsync(string adminId, CreateCustomFineDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            if (dto.Amount <= 0)
                throw new ArgumentException("Fine amount must be greater than zero.");

            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new ArgumentException("A reason is required for custom fines.");

            var fine = new Fine
            {
                UserId = dto.UserId,
                Amount = dto.Amount,
                Type = FineType.Custom,
                AdminNote = dto.Reason,
                Status = FineStatus.Unpaid,
                IssuedByAdminId = adminId,
                CreatedAt = DateTime.UtcNow
            };

            await _fineRepository.AddAsync(fine);
            await _fineRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                dto.UserId,
                NotificationType.FineIssued,
                $"A fine of {dto.Amount:C} has been issued to your account.",
                fine.Id,
                NotificationReferenceType.Fine);

            return await AdminGetFineByIdAsync(fine.Id);
        }

        public async Task<FineDto> UpdateFineAsync(string adminId, UpdateFineDto dto)
        {
            var fine = await _fineRepository.GetByIdAsync(dto.FineId);
            if (fine == null)
                throw new KeyNotFoundException("Fine not found.");

            if (fine.Status == FineStatus.Paid)
                throw new InvalidOperationException("Cannot update a paid fine.");

            if (fine.Status == FineStatus.PendingVerification)
                throw new InvalidOperationException("Cannot update a fine while payment is under review.");

            if (dto.Amount.HasValue)
                fine.Amount = dto.Amount.Value;

            if (!string.IsNullOrWhiteSpace(dto.Reason))
                fine.AdminNote = dto.Reason;

            if (dto.Status.HasValue)
                fine.Status = dto.Status.Value;

            _fineRepository.Update(fine);
            await _fineRepository.SaveChangesAsync();

            return await AdminGetFineByIdAsync(fine.Id);
        }

        //Admin voids a fine — cannot void paid fines
        public async Task VoidFineAsync(string adminId, int fineId)
        {
            var fine = await _fineRepository.GetByIdAsync(fineId);
            if (fine == null)
                throw new KeyNotFoundException("Fine not found.");

            if (fine.Status == FineStatus.Paid)
                throw new InvalidOperationException("Cannot void a paid fine.");

            if (fine.Status == FineStatus.Voided)
                throw new InvalidOperationException("Fine is already voided.");

            fine.Status = FineStatus.Voided;
            fine.PaymentMethod = null;
            fine.PaymentDescription = null;
            fine.PaymentProofImageUrl = null;
            fine.ProofSubmittedAt = null;
            fine.RejectionReason = null;

            _fineRepository.Update(fine);
            await _fineRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                fine.UserId,
                NotificationType.FineVoided,
                $"A fine of {fine.Amount:C} on your account has been voided.",
                fineId,
                NotificationReferenceType.Fine);
        }

        //User submits proof of payment
        public async Task<FineDto> SubmitPaymentProofAsync(string userId, SubmitPaymentProofDto dto)
        {
            var fine = await _fineRepository.GetByIdWithDetailsAsync(dto.FineId);
            if (fine == null)
                throw new KeyNotFoundException("Fine not found.");

            if (fine.UserId != userId)
                throw new UnauthorizedAccessException("You can only pay your own fines.");

            //Only allow proof submission if the fine is Unpaid or previously Rejected
            if (fine.Status is FineStatus.Paid or FineStatus.PendingVerification or FineStatus.Voided)
                throw new InvalidOperationException($"Cannot submit proof when fine is in status '{fine.Status}'.");

            if (string.IsNullOrWhiteSpace(dto.PaymentProofImageUrl))
                throw new ArgumentException("A payment proof image is required.");

            if (string.IsNullOrWhiteSpace(dto.PaymentDescription))
                throw new ArgumentException("A payment description is required.");

            fine.PaymentMethod = dto.PaymentMethod;
            fine.PaymentDescription = dto.PaymentDescription;
            fine.PaymentProofImageUrl = dto.PaymentProofImageUrl;
            fine.ProofSubmittedAt = DateTime.UtcNow;
            fine.RejectionReason = null; //Clear old rejection notes if this is a re-submission
            fine.Status = FineStatus.PendingVerification;

            _fineRepository.Update(fine);
            await _fineRepository.SaveChangesAsync();

            //Notify user that submission was received
            await _notificationService.SendAsync(
                userId,
                NotificationType.FinePaymentPendingVerification,
                fine.Loan != null
                    ? $"Your payment proof for the fine on '{fine.Loan.Item?.Title}' has been submitted and is under review."
                    : "Your payment proof has been submitted and is under review.",
                fine.Id,
                NotificationReferenceType.Fine);

            //Notify admins to review
            await _notificationService.SendToAdminsAsync(
                NotificationType.FinePaymentPendingVerification,
                fine.Loan != null
                    ? $"Payment proof submitted for fine #{fine.Id} on '{fine.Loan.Item?.Title}' by {fine.User?.FullName ?? userId}."
                    : $"Payment proof submitted for fine #{fine.Id} by {fine.User?.FullName ?? userId}.",
                fine.Id,
                NotificationReferenceType.Fine);

            return await GetFineByIdAsync(fine.Id, userId);
        }

        public async Task<FineDto> VerifyPaymentAsync(string adminId, int fineId, AdminFineVerifyPaymentDto dto)
        {
            var fine = await _fineRepository.GetByIdAsync(fineId);
            if (fine == null)
                throw new KeyNotFoundException("Fine not found.");

            if (fine.Status != FineStatus.PendingVerification)
                throw new InvalidOperationException("Only fines with submitted proof can be verified.");

            fine.VerifiedByAdminId = adminId;

            if (dto.IsApproved)
            {
                //Final safety check to prevent double-payment records in high-concurrency scenarios
                var alreadyPaid = await _fineRepository.ExistsPaidFineAsync(
                    fine.UserId, fine.LoanId, fine.DisputeId);

                if (alreadyPaid)
                    throw new InvalidOperationException("A paid fine already exists for this loan/dispute.");

                fine.Status = FineStatus.Paid;
                fine.PaidAt = DateTime.UtcNow;

                await _notificationService.SendAsync(
                    fine.UserId,
                    NotificationType.FinePaid,
                    $"Your payment for fine #{fine.Id} has been approved.",
                    fine.Id,
                    NotificationReferenceType.Fine);
            }
            else
            {
                //Rejection forces the user to pay and send proof again
                if (string.IsNullOrWhiteSpace(dto.RejectionReason))
                    throw new InvalidOperationException("A rejection reason is required when rejecting payment.");

                fine.Status = FineStatus.Rejected;
                fine.RejectionReason = dto.RejectionReason;

                await _notificationService.SendAsync(
                    fine.UserId,
                    NotificationType.FineRejected,
                    $"Your payment proof for fine #{fine.Id} was rejected. Reason: {dto.RejectionReason}",
                    fine.Id,
                    NotificationReferenceType.Fine);
            }

            _fineRepository.Update(fine);
            await _fineRepository.SaveChangesAsync();

            return await AdminGetFineByIdAsync(fine.Id);
        }

        public async Task<FineDto> GetFineByIdAsync(int fineId, string userId)
        {
            var fine = await _fineRepository.GetByIdWithDetailsAsync(fineId);
            if (fine == null)
                throw new KeyNotFoundException("Fine not found.");

            if (fine.UserId != userId)
                throw new UnauthorizedAccessException("You do not have access to this fine.");

            return MapToFineDto(fine, userId);
        }

        public async Task<PagedResult<FineListDto>> GetMyFinesAsync(
            string userId, FineFilter? filter, PagedRequest request)
        {
            var result = await _fineRepository.GetByUserIdAsync(userId, filter, request);
            return new PagedResult<FineListDto>
            {
                Items = result.Items.Select(f => MapToFineListDto(f, userId)).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<FineDto> AdminGetFineByIdAsync(int fineId)
        {
            var fine = await _fineRepository.GetByIdWithDetailsAsync(fineId);
            if (fine == null)
                throw new KeyNotFoundException("Fine not found.");

            return MapToFineDto(fine, null);
        }

        public async Task<PagedResult<FineListDto>> GetAllFinesAsync(
            FineFilter? filter, PagedRequest request)
        {
            var result = await _fineRepository.GetAllAsync(filter, request);
            return new PagedResult<FineListDto>
            {
                Items = result.Items.Select(f => MapToFineListDto(f)).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<PagedResult<FineListDto>> GetFinesByStatusAsync(
            FineStatus status, FineFilter? filter, PagedRequest request)
        {
            var result = await _fineRepository.GetByStatusAsync(status, filter, request);
            return new PagedResult<FineListDto>
            {
                Items = result.Items.Select(f => MapToFineListDto(f)).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<PagedResult<FineListDto>> GetPendingProofReviewAsync(
            FineFilter? filter, PagedRequest request)
        {
            var result = await _fineRepository.GetPendingProofReviewAsync(filter, request);
            return new PagedResult<FineListDto>
            {
                Items = result.Items.Select(f => MapToFineListDto(f)).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task<List<FineDto>> GetFinesByLoanIdAsync(int loanId)
        {
            var fines = await _fineRepository.GetByLoanIdAsync(loanId);
            return fines.Select(f => MapToFineDto(f, null)).ToList();
        }

        public async Task<List<FineDto>> GetFinesByDisputeIdAsync(int disputeId)
        {
            var fines = await _fineRepository.GetByDisputeIdAsync(disputeId);
            return fines.Select(f => MapToFineDto(f, null)).ToList();
        }

        public async Task<PagedResult<FineListDto>> GetAllFinesByUserIdAsync(
            string userId, FineFilter? filter, PagedRequest request, bool isAdmin = false)
        {
            if (!isAdmin)
                throw new UnauthorizedAccessException("Admin access required.");

            var result = await _fineRepository.GetByUserIdAsync(userId, filter, request);
            return new PagedResult<FineListDto>
            {
                Items = result.Items.Select(f => MapToFineListDto(f)).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }


        public async Task<FineStatsDto> GetFineStatsAsync()
        {
            var statusCounts = await _fineRepository.GetStatusCountsAsync();
            var typeCounts = await _fineRepository.GetTypeCountsAsync();
            var totalOutstandingAmount = await _fineRepository.GetOutstandingTotalAsync();
            var issuedThisMonth = await _fineRepository.GetIssuedThisMonthCountAsync();

            return new FineStatsDto
            {
                TotalUnpaid = statusCounts.GetValueOrDefault(FineStatus.Unpaid, 0)
                    + statusCounts.GetValueOrDefault(FineStatus.PendingVerification, 0),

                PendingProofReview = statusCounts.GetValueOrDefault(FineStatus.PendingVerification, 0),
                StatusBreakdown = statusCounts,
                TypeBreakdown = typeCounts,
                TotalOutstandingAmount = totalOutstandingAmount,
                IssuedThisMonth = issuedThisMonth
            };
        }

        public async Task<bool> UserHasUnpaidFinesAsync(string userId)
        {
            return await _fineRepository.HasOutstandingFinesAsync(userId);
        }

        //Mappers

        private static FineDto MapToFineDto(Fine fine, string? currentUserId)
        {
            return new FineDto
            {
                Id = fine.Id,
                UserId = fine.UserId,
                FullName = fine.User?.FullName ?? "Unknown",
                UserName = fine.User?.UserName ?? "Unknown",
                UserAvatarUrl = fine.User?.AvatarUrl,
                IsMine = currentUserId != null && fine.UserId == currentUserId,
                DisputeId = fine.DisputeId,
                LoanId = fine.LoanId,
                ItemTitle = fine.Loan?.Item?.Title,
                ItemSlug = fine.Loan?.Item?.Slug,
                Type = fine.Type,
                Status = fine.Status,
                Amount = fine.Amount,
                ItemValueAtTimeOfFine = fine.ItemValueAtTimeOfFine,
                PaymentMethod = fine.PaymentMethod,
                PaymentProofImageUrl = fine.PaymentProofImageUrl,
                PaymentDescription = fine.PaymentDescription,
                RejectionReason = fine.RejectionReason,
                IssuedByAdminId = fine.IssuedByAdminId,
                IssuedByAdminName = fine.IssuedByAdmin?.FullName,
                IssuedByAdminUserName = fine.IssuedByAdmin?.UserName,
                IssuedByAdminAvatarUrl = fine.IssuedByAdmin?.AvatarUrl,
                AdminNote = fine.AdminNote,
                VerifiedByAdminId = fine.VerifiedByAdminId,
                VerifiedByAdminName = fine.VerifiedByAdmin?.FullName,
                VerifiedByAdminUserName = fine.VerifiedByAdmin?.UserName,
                VerifiedByAdminAvatarUrl = fine.VerifiedByAdmin?.AvatarUrl,
                HasPendingAppeal = fine.Appeal != null && fine.Appeal.Status == AppealStatus.Pending,
                ActiveAppealId = fine.Appeal?.Id,
                ProofSubmittedAt = fine.ProofSubmittedAt,
                PaidAt = fine.PaidAt,
                CreatedAt = fine.CreatedAt
            };
        }

        private static FineListDto MapToFineListDto(Fine fine, string? currentUserId = null)
        {
            return new FineListDto
            {
                Id = fine.Id,
                UserId = fine.UserId,
                FullName = fine.User?.FullName ?? string.Empty,
                UserName = fine.User?.UserName ?? string.Empty,
                UserAvatarUrl = fine.User?.AvatarUrl,
                LoanId = fine.LoanId,
                DisputeId = fine.DisputeId,
                ItemTitle = fine.Loan?.Item?.Title,
                Type = fine.Type,
                Status = fine.Status,
                Amount = fine.Amount,
                HasPendingAppeal = fine.Appeal != null && fine.Appeal.Status == AppealStatus.Pending,
                CreatedAt = fine.CreatedAt,
                PaidAt = fine.PaidAt,
                IssuedByAdminId = fine.IssuedByAdminId ?? string.Empty,
                IssuedByAdminName = fine.IssuedByAdmin?.FullName ?? string.Empty,
                IssuedByAdminUsername = fine.IssuedByAdmin?.UserName ?? string.Empty,
                IssuedByAdminUserAvatarUrl = fine.IssuedByAdmin?.AvatarUrl

            };
        }

    }
}