using backend.Common;
using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using backend.ProjectionModels;
using Microsoft.AspNetCore.Identity;
using System.Runtime.ConstrainedExecution;

namespace backend.Services
{
    public class AdminUserService : IAdminUserService
    {
        private readonly IAdminUserRepository _adminRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly IItemRepository _itemRepository;
        private readonly ILoanRepository _loanRepository;
        private readonly IFineRepository _fineRepository;
        private readonly IDisputeRepository _disputeRepository;

        private readonly IScoreHistoryRepository _scoreHistoryRepository;

        public AdminUserService(IAdminUserRepository adminRepo,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IConfiguration configuration,
            IItemRepository itemRepository,
            ILoanRepository loanRepository,
            IFineRepository fineRepository,
            IDisputeRepository disputeRepository,
            IScoreHistoryRepository scoreHistoryRepository
            )
        {
            _adminRepo = adminRepo;
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
            _itemRepository = itemRepository;
            _loanRepository = loanRepository;
            _fineRepository = fineRepository;
            _disputeRepository = disputeRepository;
            _scoreHistoryRepository = scoreHistoryRepository;
        }

        public async Task<AdminUserDto> GetUserByIdAsync(string userId)
        {
            var user = await _adminRepo.GetUserByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? Roles.User;

            var unpaidFinesTotal = await _fineRepository.GetOutstandingTotalByUserAsync(userId);

            return MapToAdminUserDto(user, role, unpaidFinesTotal);
        }

        //Duplicate? ALERT
        public async Task<AdminUserDto> GetUserByIdWIthDetailsAsync(string userId)
        {
            var userWithCounts = await _adminRepo.GetUserByIdWithDetailsAsync(userId);
            if (userWithCounts == null) throw new KeyNotFoundException("User not found.");

            var roles = await _userManager.GetRolesAsync(userWithCounts.User);
            var role = roles.FirstOrDefault() ?? Roles.User;
            var unpaidFinesTotal = await _fineRepository.GetOutstandingTotalByUserAsync(userId);

            return MapToAdminUserCountsDto(userWithCounts, role, unpaidFinesTotal);
        }

        public async Task<AdminUserDto?> GetUserByIdIgnoreFiltersAsync(string userId)
        {
            var user = await _adminRepo.GetUserByIdIgnoreFiltersAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? Roles.User;
            var unpaidFinesTotal = await _fineRepository.GetOutstandingTotalByUserAsync(userId);

            return MapToAdminUserDto(user, role, unpaidFinesTotal);

        }

        public async Task<PagedResult<AdminUserDto>> GetAllBannedUsersAsync(UserFilter? filter, PagedRequest request, bool tempBansOnly = false)
        {
            filter ??= new UserFilter();
            
            var result = await _adminRepo.GetAllBannedUsersAsync(filter, request, tempBansOnly);

            //Batch fetch outstanding fines for the entire page to avoid N+1 queries
            var userIds = result.Items.Select(u => u.User.Id).ToList();
            var fineTotals = await _fineRepository.GetOutstandingTotalsByUsersAsync(userIds);

            var dtos = result.Items
                .Select(u => MapToAdminUserDto(u.User, u.Role, fineTotals.GetValueOrDefault(u.User.Id, 0)))
                .ToList();


            return new PagedResult<AdminUserDto>
            {
                Items = dtos,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }


        public async Task<AdminUserDto> AdminEditUserAsync(
            string targetUserId,
            string adminId,
            AdminEditUserDto dto)
        {
            var userWithCounts = await _adminRepo.GetUserByIdWithDetailsAsync(targetUserId);
            if (userWithCounts == null)
                throw new KeyNotFoundException("User not found.");

            var user = userWithCounts.User;
            string? oldEmail = user.Email;

            //Queue emails to be sent only after the database transaction succeeds
            var postCommitActions = new List<Func<Task>>();

            //Identity operations
            if (!string.IsNullOrWhiteSpace(dto.Username) && !string.Equals(user.UserName, dto.Username, StringComparison.OrdinalIgnoreCase))
            {
                var taken = await _userManager.FindByNameAsync(dto.Username.Trim());
                if (taken != null && taken.Id != targetUserId)
                    throw new ArgumentException("That username is already taken.");

                var res = await _userManager.SetUserNameAsync(user, dto.Username.Trim());
                if (!res.Succeeded) throw new ArgumentException(string.Join(", ", res.Errors.Select(e => e.Description)));
            }

            if (!string.IsNullOrWhiteSpace(dto.Email) && !string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailTaken = await _userManager.FindByEmailAsync(dto.Email.Trim());
                if (emailTaken != null && emailTaken.Id != targetUserId)
                    throw new ArgumentException("That email is already in use.");

                var res = await _userManager.SetEmailAsync(user, dto.Email.Trim());
                if (!res.Succeeded) throw new ArgumentException(string.Join(", ", res.Errors.Select(e => e.Description)));

                postCommitActions.Add(async () =>
                {
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var link = $"{_configuration["App:BaseUrl"]}/api/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";

                    await _emailService.SendEmailAsync(user.Email!, "Action Required: Confirm your new RentIt email",
                        $"<h2>Email address updated</h2><p>Please confirm your email by clicking <a href='{link}'>here</a>.</p>");

                    if (!string.IsNullOrEmpty(oldEmail))
                    {
                        await _emailService.SendEmailAsync(oldEmail, "Security Alert: RentIt email changed",
                            $"<p>The email for your account was changed to {user.Email}. If this wasn't you, contact us immediately.</p>");
                    }
                });
            }

            if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var res = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
                if (!res.Succeeded) throw new ArgumentException("Failed to reset password.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                var allowed = new[] { Roles.User, Roles.Admin };
                if (!allowed.Contains(dto.Role, StringComparer.OrdinalIgnoreCase))
                    throw new ArgumentException("Invalid role.");

                var currentRoles = await _userManager.GetRolesAsync(user);
                if (!currentRoles.Contains(dto.Role, StringComparer.OrdinalIgnoreCase))
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    await _userManager.AddToRoleAsync(user, dto.Role);
                }
            }


            //EF TRANSACTION
            await _adminRepo.ExecuteInTransactionAsync(async () =>
            {
                if (!string.IsNullOrWhiteSpace(dto.FullName)) user.FullName = dto.FullName.Trim();
                if (dto.Address != null) user.Address = dto.Address.Trim();
                if (dto.Latitude.HasValue) user.Latitude = dto.Latitude;
                if (dto.Longitude.HasValue) user.Longitude = dto.Longitude;
                if (dto.Gender != null) user.Gender = dto.Gender.Trim();
                if (dto.AvatarUrl != null) user.AvatarUrl = dto.AvatarUrl.Trim();
                if (dto.IsVerified.HasValue) user.IsVerified = dto.IsVerified.Value;

                if (dto.Score.HasValue && dto.Score.Value != user.Score)
                {
                    if (dto.Score.Value < 0 || dto.Score.Value > 100) throw new ArgumentException("Score must be 0-100.");

                    await _scoreHistoryRepository.AddAsync(new ScoreHistory
                    {
                        UserId = user.Id,
                        PointsChanged = dto.Score.Value - user.Score,
                        ScoreAfterChange = dto.Score.Value,
                        Reason = ScoreChangeReason.AdminAdjustment,
                        Note = dto.ScoreNote ?? "Admin adjustment",
                        CreatedAt = DateTime.UtcNow
                    });
                    user.Score = dto.Score.Value;
                }

                _adminRepo.Update(user);
            });

            //post connit actions
            foreach (var action in postCommitActions)
            {
                try { await action(); }
                catch
                {
                    //log the exception
                }
            }

            //Final Load & Return
            var finalData = await _adminRepo.GetUserByIdWithDetailsAsync(targetUserId)
                ?? throw new KeyNotFoundException("User not found.");

            var rolesFinal = await _userManager.GetRolesAsync(finalData.User);
            var unpaidFinesTotal = await _fineRepository.GetOutstandingTotalByUserAsync(targetUserId);

            return MapToAdminUserCountsDto(finalData, rolesFinal.FirstOrDefault() ?? Roles.User, unpaidFinesTotal);
        }


        public async Task<AdminDeleteResultDto> AdminSoftDeleteUserAsync(string targetUserId, string adminId, string? note = null)
        {
            if (string.IsNullOrEmpty(adminId))
                throw new ArgumentException("AdminId is required.");

            await _adminRepo.ExecuteInTransactionAsync(async () =>
            {
                var user = await _adminRepo.GetUserByIdAsync(targetUserId);
                if (user == null) throw new KeyNotFoundException("User not found.");

                if (await _loanRepository.HasOngoingLoansAsBorrower(targetUserId))
                    throw new InvalidOperationException("Cannot delete: user has ongoing loans as a borrower.");

                if (await _loanRepository.HasOngoingLoansAsOwner(targetUserId))
                    throw new InvalidOperationException("Cannot delete: someone is currently borrowing one of this user's items.");

                if (await _disputeRepository.HasActiveDisputeByUserIdAsync(targetUserId))
                    throw new InvalidOperationException("Cannot delete: user is involved in active disputes.");


                var allUserItems = await _itemRepository.GetByOwnerIdAsync(targetUserId) ?? new List<Item>();
                foreach (var item in allUserItems)
                {
                    item.IsActive = false;
                    item.Status = ItemStatus.Deleted;
                    item.Availability = ItemAvailability.Unavailable;
                }

                SoftDelete(user, adminId, note ?? "Deleted");
                _adminRepo.Update(user);

            });
            return new AdminDeleteResultDto { Success = true };

        }

        public async Task<PagedResult<AdminUserDto>> GetUsersAsync(UserFilter? filter, PagedRequest request)
        {
            filter ??= new UserFilter();
            var result = await _adminRepo.GetUsersAsync(filter, request);

            var userIds = result.Items.Select(x => x.User.Id).ToList();
            var fineTotals = await _fineRepository.GetOutstandingTotalsByUsersAsync(userIds);

            return new PagedResult<AdminUserDto>
            {
                Items = result.Items
                        .Select(x => MapToAdminUserDto(x.User, x.Role, fineTotals.GetValueOrDefault(x.User.Id, 0)))
                        .ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };

        }

        public async Task<PagedResult<AdminUserDto>> GetAllUsersIncludingDeletedAsync(UserFilter? filter, PagedRequest request)
        {
            filter ??= new UserFilter();

            filter.IncludeDeleted = true;

            var result = await _adminRepo.GetAllUsersIncludingDeletedAsync(filter, request);

            var userIds = result.Items.Select(x => x.User.Id).ToList();
            var fineTotals = await _fineRepository.GetOutstandingTotalsByUsersAsync(userIds);

            return new PagedResult<AdminUserDto>
            {
                Items = result.Items
                    .Select(x => MapToAdminUserDto(x.User, x.Role, fineTotals.GetValueOrDefault(x.User.Id, 0)))
                    .ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        public async Task BanUserAsync(string targetUserId, string adminId, BanUserDto dto)
        {
            var user = await _adminRepo.GetUserByIdAsync(targetUserId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            if (string.IsNullOrEmpty(adminId))
                throw new ArgumentException("AdminId is required for banning a user.");

            var targetRoles = await _userManager.GetRolesAsync(user);
            if (targetRoles.Contains(Roles.Admin))
                throw new InvalidOperationException("Admins cannot be banned.");


            if (dto.BanExpiresAt.HasValue && dto.BanExpiresAt.Value <= DateTime.UtcNow)
                throw new ArgumentException("Ban expiry must be in the future.");

            user.IsBanned = true;
            user.BannedAt = DateTime.UtcNow;
            user.BanReason = dto.Reason;
            user.BannedByAdminId = adminId;
            user.BanExpiresAt = dto.BanExpiresAt;
            user.RefreshToken = null;
            user.SecurityStamp = Guid.NewGuid().ToString(); //Log them out everywhere

            //Identity lockout
            user.LockoutEnabled = true;
            //user.LockoutEnd = banExpiresAt.HasValue ? DateTimeOffset.UtcNow.Add(duration.Value) : DateTimeOffset.MaxValue;
            user.LockoutEnd = dto.BanExpiresAt.HasValue ? new DateTimeOffset(dto.BanExpiresAt.Value)
                : DateTimeOffset.MaxValue;

            var banHistory = new UserBanHistory
            {
                UserId = targetUserId,
                AdminId = adminId,
                IsBanned = true,
                Reason = dto.Reason,
                BannedAt = DateTime.UtcNow,
                BanExpiresAt = dto.BanExpiresAt
            };
            user.BanHistory.Add(banHistory);


            _adminRepo.Update(user);
            await _adminRepo.SaveChangesAsync();

            //Send email notification to user

            if (!string.IsNullOrEmpty(user.Email))
            {
                string durationText = dto.BanExpiresAt.HasValue ? $"until {dto.BanExpiresAt.Value:yyyy-MM-dd HH:mm} UTC"
                    : "permanently";

                await _emailService.SendEmailAsync(
                    user.Email,
                    "Account Banned Notification",
                    $"<h2>Hello {user.FullName},</h2>" +
                    $"<p>Your account has been banned {durationText}.</p>" +
                    $"<p>Reason: {dto.Reason}</p>" +
                    $"<p>If you think this is a mistake, please contact support.</p>"
                );
            }
        }

        public async Task UnbanUserAsync(string targetUserId, string adminId, UnbanUserDto dto)
        {
            var user = await _adminRepo.GetUserByIdAsync(targetUserId);
            if (user == null) throw new KeyNotFoundException("User not found.");


            if (string.IsNullOrEmpty(adminId))
                throw new ArgumentException("AdminId is required for banning a user.");


            if (!user.IsBanned)
                throw new InvalidOperationException("User is not currently banned.");

            //Reset ban info
            user.IsBanned = false;
            user.BannedAt = null;
            user.BanReason = null;
            user.BanExpiresAt = null;
            user.BannedByAdminId = null;

            //Identity lockout
            user.LockoutEnabled = false;
            user.LockoutEnd = null;

            //Log unban in history
            user.BanHistory.Add(new UserBanHistory
            {
                UserId = targetUserId,
                AdminId = adminId,
                IsBanned = false,
                Reason = dto.Reason ?? "Unbanned By Admin.",
                BannedAt = DateTime.UtcNow,
                BanExpiresAt = null
            });
     
            _adminRepo.Update(user);
            await _adminRepo.SaveChangesAsync();

            if (!string.IsNullOrEmpty(user.Email))
            {
                await _emailService.SendEmailAsync(
                    user.Email,
                    "You have been unbanned",
                    $"<h2>Hello {user.FullName},</h2>" +
                    $"<p>Your account has been unbanned and you can log in again.</p>"
                );
            }
        }

        //Mappers
        private AdminUserDto MapToAdminUserDto(ApplicationUser user, string role, decimal unpaidFinesTotal)
        {
            return new AdminUserDto
            {
                Id = user.Id,
                FullName = user.FullName ?? string.Empty,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Gender = user.Gender,
                Bio = user.Bio,
                Address = user.Address ?? string.Empty,
                Latitude = user.Latitude,
                Longitude = user.Longitude,
                AvatarUrl = user.AvatarUrl,
                Age = user.Age,
                Role = role,
                Score = user.Score,
                IsVerified = user.IsVerified,
                MembershipDate = user.MembershipDate,
                UnpaidFinesTotal = unpaidFinesTotal,
                EmailConfirmed = user.EmailConfirmed,
                IsBanned = user.IsBanned,
                BannedAt = user.BannedAt,
                BanReason = user.BanReason,
                BanExpiresAt = user.BanExpiresAt,
                BannedByAdminId = user.BannedByAdminId,
                BannedByAdminName = user.BannedByAdmin?.FullName,
                BannedByAdminAvatarUrl = user.BannedByAdmin?.AvatarUrl,
                IsDeleted = user.IsDeleted,
                DeletedAt = user.DeletedAt,
                DeletionNote = user.DeletionNote,
                DeletedByAdminId = user.DeletedByAdminId,
                DeletedByAdminName = user.DeletedByAdmin?.FullName,
                DeletedByAdminAvatarUrl = user.DeletedByAdmin?.AvatarUrl,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }
        
        private AdminUserDto MapToAdminUserCountsDto(UserWithCounts userWithCounts, string role, decimal unpaidFinesTotal)
        {
            var user = userWithCounts.User;

            return new AdminUserDto
            {
                Id = user.Id,
                FullName = user.FullName ?? string.Empty,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,

                PhoneNumber = user.PhoneNumber,
                Gender = user.Gender,
                Bio = user.Bio,
                Address = user.Address ?? string.Empty,
                Latitude = user.Latitude,
                Longitude = user.Longitude,
                AvatarUrl = user.AvatarUrl,
                Age = user.Age,
                Role = role,

                Score = user.Score,
                IsVerified = user.IsVerified,
                MembershipDate = user.MembershipDate,
                UnpaidFinesTotal = unpaidFinesTotal,
                EmailConfirmed = user.EmailConfirmed,

                IsBanned = user.IsBanned,
                BannedAt = user.BannedAt,
                BanReason = user.BanReason,
                BanExpiresAt = user.BanExpiresAt,
                BannedByAdminId = user.BannedByAdminId,
                BannedByAdminName = user.BannedByAdmin?.FullName,
                BannedByAdminAvatarUrl = user.BannedByAdmin?.AvatarUrl,

                IsDeleted = user.IsDeleted,
                DeletedAt = user.DeletedAt,
                DeletionNote = user.DeletionNote,
                DeletedByAdminId = user.DeletedByAdminId,
                DeletedByAdminName = user.DeletedByAdmin?.FullName,
                DeletedByAdminAvatarUrl = user.DeletedByAdmin?.AvatarUrl,
                
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,

                TotalOwnedItems = userWithCounts.OwnedItemsCount,
                TotalBorrowedLoans = userWithCounts.BorrowedLoansCount,
                TotalGivenLoans = userWithCounts.GivenLoansCount,
                TotalFines = userWithCounts.FinesCount,
                TotalScoreHistory = userWithCounts.ScoreHistoryCount,
                TotalAppeals = userWithCounts.AppealsCount,
                TotalSupportThreads = userWithCounts.SupportThreadsCount,
                TotalItemReviews = userWithCounts.ItemReviewsCount,
                TotalReviewsGiven = userWithCounts.ReviewsGivenCount,
                TotalReviewsReceived = userWithCounts.ReviewsReceivedCount,
                TotalVerificationRequests = userWithCounts.VerificationRequestsCount,
                TotalBanHistory = userWithCounts.BanHistoryCount,
                TotalInitiatedDisputes = userWithCounts.InitiatedDisputesCount,
                TotalReceivedDisputes = userWithCounts.ReceivedDisputesCount,

                //Admin Activity Counts
                TotalDisputesResolved = userWithCounts.ResolvedDisputesCount,
                TotalAppealsResolved = userWithCounts.ResolvedAppealsCount,
                TotalVerificationRequestsReviewed = userWithCounts.ReviewedVerificationRequestsCount,
                TotalSupportThreadsClaimed = userWithCounts.ClaimedSupportThreadsCount,
            };



        
        }
       
        private static void SoftDelete(ApplicationUser user, string adminId, string note)
        {
            var suffix = DateTime.UtcNow.Ticks.ToString();

            user.UserName = $"deleted_{suffix}";
            user.NormalizedUserName = user.UserName.ToUpper();
            user.Email = $"deleted_{suffix}@rentit.local";
            user.NormalizedEmail = user.Email.ToUpper();
            user.FullName = "Deleted User";
            user.Bio = null;
            user.Address = "Deleted User Address";
            user.Latitude = null;
            user.Longitude = null;
            user.AvatarUrl = null;
            user.PasswordHash = null;
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.DeletedByAdminId = adminId;
            user.DeletionNote = note;

        }
    
    
    }
}