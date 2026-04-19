using backend.Common;
using backend.Configuration;
using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using backend.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace backend.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILoanRepository _loanRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IFineRepository _fineRepository;
        private readonly IDisputeRepository _disputeRepository;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ScoreThresholdOptions _scoreOptions;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public UserService(
            IUserRepository userRepository,
            ILoanRepository loanRepository,
            IItemRepository itemRepository,
            IFineRepository fineRepository,
            IDisputeRepository disputeRepository,
            UserManager<ApplicationUser> userManager,
            IOptions<ScoreThresholdOptions> scoreThresholdOptions,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _scoreOptions = scoreThresholdOptions.Value;
            _emailService = emailService;
            _configuration = configuration;
            _loanRepository = loanRepository;
            _disputeRepository = disputeRepository;
            _itemRepository = itemRepository;
            _fineRepository = fineRepository;
        }

        public async Task<UserProfileDto> GetProfileAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();

            //Total debt is calculated in real-time to ensure profile accuracy
            var unpaid = await _fineRepository.GetOutstandingTotalByUserAsync(userId);
            var totalCompletedLoans = await _loanRepository.GetAllCompletedLoansCountByUserIdAsync(userId);

            return new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName ?? string.Empty,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Gender = user.Gender,
                Role = role ?? string.Empty,
                Bio = user.Bio,
                Address = user.Address,
                Latitude = user.Latitude,
                Longitude = user.Longitude,
                DateOfBirth = user.DateOfBirth,
                Age = user.Age,
                AvatarUrl = user.AvatarUrl,
                Score = user.Score,
                UnpaidFinesTotal = unpaid,
                IsVerified = user.IsVerified,
                IsBanned = user.IsBanned,
                TotalCompletedLoans = totalCompletedLoans,
                BorrowingStatus = GetBorrowingStatus(user), 
                MembershipDate = user.MembershipDate,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
            };

        }

        public async Task<UserPublicProfileDto> GetPublicProfileAsync(string userId, string currentUserId)
        {
            var profile = await _userRepository.GetPublicProfileByIdAsync(userId, currentUserId);

            if (profile == null)
                throw new KeyNotFoundException("User not found or is blocked.");

            return profile;

        }

        public async Task<UserProfileDto> UpdateProfileAsync(string userId, UpdateProfileDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            bool userManagerChanged = false;

            //Username change
            if (!string.IsNullOrWhiteSpace(dto.UserName) && dto.UserName != user.UserName)
            {

                var newUsername = dto.UserName.Trim();
                if (!string.Equals(user.UserName, newUsername, StringComparison.OrdinalIgnoreCase))
                {
                    //Check uniqueness
                    var taken = await _userManager.FindByNameAsync(newUsername);
                    if (taken != null && taken.Id != userId)
                        throw new InvalidOperationException("Username is already taken.");

                    var result = await _userManager.SetUserNameAsync(user, newUsername);
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Failed to update username: {errors}");
                    }

                    userManagerChanged = true;
                }
            }

            //Handle email change
            bool emailChanged = false;
            if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
            {
                var newEmail = dto.Email.Trim();
                if (!string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
                {
                    //Check uniqueness
                    var emailTaken = await _userManager.FindByEmailAsync(newEmail);
                    if (emailTaken != null && emailTaken.Id != userId)
                        throw new InvalidOperationException("Email is already in use.");

                    var result = await _userManager.SetEmailAsync(user, newEmail);
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Failed to update email: {errors}");
                    }

                    user.EmailConfirmed = false; //Force re-verification
                    emailChanged = true;
                    userManagerChanged = true;
                }

            }

            //Reload if UserManager modified the user
            if (userManagerChanged)
            {
                user = await _userRepository.GetByIdAsync(userId);
                if (user == null) throw new KeyNotFoundException("User not found after UserManager operations.");
            }


            if (!string.IsNullOrWhiteSpace(dto.FullName)) user.FullName = dto.FullName;
            if (!string.IsNullOrWhiteSpace(dto.Address)) user.Address = dto.Address;
            if (dto.Latitude.HasValue) user.Latitude = dto.Latitude.Value;
            if (dto.Longitude.HasValue) user.Longitude = dto.Longitude.Value;
            if (dto.Gender != null) user.Gender = dto.Gender;
            if (dto.Bio != null) user.Bio = dto.Bio;
            if (dto.AvatarUrl != null) user.AvatarUrl = dto.AvatarUrl;


            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            //Send verification email if email changed
            if (emailChanged)
            {
                try
                {
                    var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var confirmLink = $"{_configuration["App:BaseUrl"]}/api/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(confirmToken)}";

                    await _emailService.SendEmailAsync(
                        user.Email!,
                        "Confirm your new email address",
                        $"<h2>Hello {user.FullName},</h2>" +
                        $"<p>Your email address has been updated.</p>" +
                        $"<p>Please verify your new email by clicking the link below:</p>" +
                        $"<p><a href='{confirmLink}'>Confirm Email</a></p>" +
                        $"<p>If you did not request this change, please contact support immediately.</p>"
                    );
                }
                catch (Exception ex)
                {
                   //_logger.LogError(ex, "Failed to send email verification to {Email}", user.Email);
                }
            }

            return await GetProfileAsync(userId);
        }

        public async Task DeleteAccountAsync(string userId, DeleteAccountDto dto)
        {
            //Transactional wrapper ensures that user soft delete and item deactivation happen atomically
            await _userRepository.ExecuteInTransactionAsync(async () =>
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null) throw new KeyNotFoundException("User not found.");

                var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
                if (!passwordValid) throw new UnauthorizedAccessException("Incorrect password.");

                var hasDebt = await _fineRepository.HasOutstandingFinesAsync(userId);
                if (hasDebt)
                    throw new InvalidOperationException("You have unpaid fines. Please settle them before deleting your account.");

                if (await _loanRepository.HasOngoingLoansAsBorrower(userId))
                    throw new InvalidOperationException("You have ongoing loans as a borrower. Return all items before deleting your account.");

                if (await _loanRepository.HasOngoingLoansAsOwner(userId))
                    throw new InvalidOperationException("Someone is currently borrowing one of your items. Wait for all loans to complete before deleting your account.");

                if (await _disputeRepository.HasActiveDisputeByUserIdAsync(userId))
                    throw new InvalidOperationException("You have active disputes. Wait for it to resolve before deleting your account");


                var allUserItems = await _itemRepository.GetByOwnerIdAsync(userId) ?? new List<Item>();

                foreach (var item in allUserItems)
                {
                    item.IsActive = false;
                    item.Status = ItemStatus.Deleted;
                    item.Availability = ItemAvailability.Unavailable;
                    _itemRepository.Update(item);
                }


                SoftDelete(user);
                _userRepository.Update(user);
            });
        }


        public async Task<PagedResult<UserProfileDto>> SearchUsersAsync(UserFilter? filter, PagedRequest request,
            string currentUserId)
        {
            var pagedUsers = await _userRepository.SearchByUsernameOrEmailAsync(filter, request, currentUserId);

            var userDtos = pagedUsers.Items.Select(u => new UserProfileDto
            {
                Id = u.Id,
                Username = u.UserName,
                FullName = u.FullName,
                AvatarUrl = u.AvatarUrl,
                Score = u.Score,
                IsVerified = u.IsVerified,
                MembershipDate = u.MembershipDate,
                BorrowingStatus = GetBorrowingStatus(u) //Reuse the helper we built
            }).ToList();

            return new PagedResult<UserProfileDto>
            {
                Items = userDtos,
                TotalCount = pagedUsers.TotalCount,
                Page = pagedUsers.Page,
                PageSize = pagedUsers.PageSize
            };
        }

        public async Task EnsureUserCanBorrowAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found.");
            if (user.IsDeleted) throw new InvalidOperationException("Account is deleted.");
            if (user.IsBanned) throw new InvalidOperationException("Account is banned.");
            if (user.Score < _scoreOptions.BlockedBelow)
                throw new InvalidOperationException("Your score is too low to borrow items.");
        }

        public async Task EnsureUserIsActiveAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new KeyNotFoundException("User not found.");

            if (user.IsDeleted) throw new InvalidOperationException("Account is deleted.");
            if (user.IsBanned) throw new InvalidOperationException("Account is banned.");
        }

        //For landing page
        public async Task<int> GetTotalUsersCountAsync()
        {
            return await _userRepository.GetTotalUsersCountAsync();
        }


        private static void SoftDelete(ApplicationUser user)
        {
            var suffix = DateTime.UtcNow.Ticks.ToString();

            user.UserName = $"deleted_{suffix}";
            user.NormalizedUserName = user.UserName.ToUpper();
            user.Email = $"deleted_{suffix}@rentit.local";
            user.NormalizedEmail = user.Email.ToUpper();
            user.FullName = "Deleted User";
            user.PhoneNumber = null;
            user.Bio = null;
            user.Address = "Deleted Address";
            user.Latitude = null;
            user.Longitude = null;
            user.AvatarUrl = null;
            user.Gender = null;
            user.PasswordHash = null;
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.IsBanned = false;
            user.EmailConfirmed = false;
        }

        private BorrowingStatus GetBorrowingStatus(ApplicationUser user)
        {
            if (user == null) return BorrowingStatus.Free;


            if (user.IsBanned || user.Score < _scoreOptions.BlockedBelow)
                return BorrowingStatus.Blocked;

            if (user.Score <= _scoreOptions.AdminApprovalBelowOrEqual)
                return BorrowingStatus.AdminApproval;

            return BorrowingStatus.Free;
        }
   



        //Mapper unused
        private UserProfileDto MapToUserProfileDto(ApplicationUser user, string? role)
        {
            return new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName ?? string.Empty,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,        
                PhoneNumber = user.PhoneNumber,
                Gender = user.Gender,
                Role = role ?? string.Empty,
                Bio = user.Bio,
                Address = user.Address,
                Latitude = user.Latitude,
                Longitude = user.Longitude,
                DateOfBirth = user.DateOfBirth,
                Age = user.Age,
                AvatarUrl = user.AvatarUrl,
                Score = user.Score,
                IsVerified = user.IsVerified,
                IsBanned = user.IsBanned,
                BorrowingStatus = GetBorrowingStatus(user),
                MembershipDate = user.MembershipDate,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
            };
        }

    }
}