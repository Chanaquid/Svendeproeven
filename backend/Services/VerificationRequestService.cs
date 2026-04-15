using backend.Dtos;
using backend.Interfaces;
using backend.Models;

namespace backend.Services
{
    public class VerificationRequestService : IVerificationRequestService
    {
        private readonly IVerificationRequestRepository _verificationRepository;
        private readonly IUserRepository _userRepository;
        private readonly INotificationService _notificationService;

        public VerificationRequestService(
            IVerificationRequestRepository verificationRepository,
            IUserRepository userRepository,
            INotificationService notificationService)
        {
            _verificationRepository = verificationRepository;
            _userRepository = userRepository;
            _notificationService = notificationService;
        }

        public async Task<VerificationRequestDto> SubmitRequestAsync(
            string userId,
            CreateVerificationRequestDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            if (user.IsVerified)
                throw new InvalidOperationException("Your account is already verified.");

            if (await _verificationRepository.HasPendingRequestAsync(userId))
                throw new InvalidOperationException("You already have a pending verification request.");

            var request = new VerificationRequest
            {
                UserId = userId,
                DocumentType = dto.DocumentType,
                DocumentUrl = dto.DocumentUrl.Trim(),
                Status = VerificationStatus.Pending,
                SubmittedAt = DateTime.UtcNow
            };

            await _verificationRepository.AddAsync(request);
            await _verificationRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                userId,
                NotificationType.VerificationSubmitted,
                "Your verification request has been submitted and will be reviewed by an admin shortly.",
                request.Id,
                NotificationReferenceType.Verification
            );

            var created = await _verificationRepository.GetByIdWithDetailsAsync(request.Id);
            return MapToDto(created!);
        }

        public async Task<PagedResult<VerificationRequestDto>> GetMyRequestsAsync(
            string userId,
            VerificationRequestFilter? filter,
            PagedRequest request)
        {
            var paged = await _verificationRepository.GetByUserIdAsync(userId, filter, request);
            return MapPagedResult(paged);
        }

        public async Task<VerificationRequestDto> GetByIdAsync(int id, string userId, bool isAdmin)
        {
            var request = await _verificationRepository.GetByIdWithDetailsAsync(id)
                ?? throw new KeyNotFoundException("Verification request not found.");

            if (!isAdmin && request.UserId != userId)
                throw new UnauthorizedAccessException("You cannot view this verification request.");

            return MapToDto(request);
        }

        //Admin

        public async Task<VerificationRequestDto> DecideAsync(
            int id,
            string adminId,
            AdminDecideVerificationRequestDto dto)
        {
            var request = await _verificationRepository.GetByIdWithDetailsAsync(id)
                ?? throw new KeyNotFoundException("Verification request not found.");

            var admin = await _userRepository.GetByIdAsync(adminId)
                ?? throw new KeyNotFoundException("Admin not found.");

            if (request.Status != VerificationStatus.Pending)
                throw new InvalidOperationException("This request has already been reviewed.");

            if (dto.Status == VerificationStatus.Pending)
                throw new ArgumentException("Decision status cannot be Pending.");

            if (dto.Status == VerificationStatus.Rejected && string.IsNullOrWhiteSpace(dto.AdminNote))
                throw new ArgumentException("A reason is required when rejecting a verification request.");

            request.Status = dto.Status;
            request.ReviewedByAdminId = adminId;
            request.ReviewedByAdmin = admin;
            request.AdminNote = dto.AdminNote?.Trim();
            request.ReviewedAt = DateTime.UtcNow;

            if (dto.Status == VerificationStatus.Approved)
            {
                var user = request.User;
                user.IsVerified = true;
                _userRepository.Update(user);
            }

            _verificationRepository.Update(request);
            await _userRepository.SaveChangesAsync();
            await _verificationRepository.SaveChangesAsync();

            await _notificationService.SendAsync(
                request.UserId,
                dto.Status == VerificationStatus.Approved
                    ? NotificationType.VerificationApproved
                    : NotificationType.VerificationRejected,
                dto.Status == VerificationStatus.Approved
                    ? "Your verification request has been approved. Your account is now verified."
                    : $"Your verification request was rejected. Reason: {dto.AdminNote}",
                request.Id,
                NotificationReferenceType.Verification
            );

            return MapToDto(request);
        }

        public async Task<PagedResult<VerificationRequestDto>> GetAllAsync(
            VerificationRequestFilter? filter,
            PagedRequest request)
        {
            var paged = await _verificationRepository.GetAllAsync(filter, request);
            return MapPagedResult(paged);
        }

        public async Task<PagedResult<VerificationRequestDto>> GetByUserIdAsync(
            string userId,
            VerificationRequestFilter? filter,
            PagedRequest request)
        {
            _ = await _userRepository.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            var paged = await _verificationRepository.GetByUserIdAsync(userId, filter, request);
            return MapPagedResult(paged);
        }

        //Helpers

        private static VerificationRequestDto MapToDto(VerificationRequest v)
        {
            return new VerificationRequestDto
            {
                Id = v.Id,
                UserId = v.UserId,
                FullName = v.User?.FullName ?? string.Empty,
                UserName = v.User?.UserName ?? string.Empty,
                UserAvatarUrl = v.User?.AvatarUrl,
                DocumentType = v.DocumentType,
                DocumentUrl = v.DocumentUrl,
                Status = v.Status,
                AdminNote = v.AdminNote,
                ReviewedByAdminId = v.ReviewedByAdminId,
                ReviewedByAdminName = v.ReviewedByAdmin?.FullName,
                ReviewedByAdminAvatarUrl = v.ReviewedByAdmin?.AvatarUrl,
                SubmittedAt = v.SubmittedAt,
                ReviewedAt = v.ReviewedAt
            };
        }

        private static PagedResult<VerificationRequestDto> MapPagedResult(PagedResult<VerificationRequest> source)
        {
            return new PagedResult<VerificationRequestDto>
            {
                Items = source.Items.Select(MapToDto).ToList(),
                TotalCount = source.TotalCount,
                Page = source.Page,
                PageSize = source.PageSize
            };
        }
    }
}