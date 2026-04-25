using backend.Common;
using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace backend.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _configuration = configuration;
        }

        //Register user
        public async Task<RegisterUserResponseDto> RegisterAsync(RegisterUserRequestDto dto)
        {
            var existing = await _userManager.FindByEmailAsync(dto.Email.Trim().ToLower());
            if (existing != null)
            {
                //Just in case the soft delete email doesnt work
                if (existing.IsDeleted)
                    throw new ArgumentException("This email was previously used on a deleted account. Please contact support.");

                throw new ArgumentException("An account with this email already exists.");
            }

            var usernameTaken = await _userManager.FindByNameAsync(dto.Username.Trim());
            if (usernameTaken != null)
                throw new ArgumentException("Username is already taken.");

            var user = new ApplicationUser
            {
                FullName = dto.FullName.Trim(),
                Email = dto.Email.Trim().ToLower(),
                UserName = dto.Username.Trim(),
                Address = dto.Address.Trim(),
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender?.Trim(),
                Bio = dto.Bio?.Trim(),
                AvatarUrl = dto.AvatarUrl?.Trim(),
                Score = 100, 
                MembershipDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new ArgumentException(errors);
            }

            //Assign default role
            await _userManager.AddToRoleAsync(user, Roles.User);

            //Send confirmation email
            var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmLink = $"{_configuration["App:FrontendUrl"]}/auth/confirm?userId={user.Id}&token={Uri.EscapeDataString(confirmToken)}";

            await _emailService.SendEmailAsync(
                user.Email,
                "Confirm your RentIt account",
                $"<h2>Welcome to RentIt, {user.FullName}!</h2>" +
                $"<p>Please confirm your email address by clicking the link below:</p>" +
                $"<p><a href='{confirmLink}'>Confirm Email</a></p>" +
                $"<p>If you did not create an account, you can ignore this email.</p>"
            );

            return new RegisterUserResponseDto
            {
                UserId = user.Id,
                Username = user.UserName,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl,
                //Token and RefreshToken intentionally left empty until email is confirmed
            };
        }

        //Confirm email
        public async Task<bool> ConfirmEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.ConfirmEmailAsync(user, token);
            return result.Succeeded;
        }

        //LOgin user
        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email.Trim().ToLower());

            if (user == null) throw new ArgumentException("Invalid email or password.");
            if (user.IsDeleted) throw new ArgumentException("This account has been deleted."); //Just in case soft delete fails

            if (user.IsBanned)
            {
                if (user.BanExpiresAt.HasValue)
                    throw new ArgumentException($"TEMP_BAN|{DateTime.SpecifyKind(user.BanExpiresAt.Value, DateTimeKind.Utc):O}|{user.BanReason}");
                else
                    throw new ArgumentException($"PERM_BAN|{user.BanReason}");
            }

            if (!await _userManager.IsEmailConfirmedAsync(user)) throw new ArgumentException("Please confirm your email before logging in.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);

            if (result.IsLockedOut) throw new ArgumentException("Account is locked. Please try again later.");
            if (!result.Succeeded) throw new ArgumentException("Invalid email or password.");

            return await BuildAuthResponseAsync(user);
        }

        //Refresh token
        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto)
        {
            //Hash the incoming token and look up by hash - never store plain text
            var tokenHash = HashToken(dto.RefreshToken);

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == tokenHash);

            if (user == null) throw new UnauthorizedAccessException("Invalid refresh token.");
            if (user.IsDeleted) throw new UnauthorizedAccessException("This account has been deleted.");
            if (user.IsBanned) throw new UnauthorizedAccessException("This account has been banned.");
            if (user.RefreshTokenExpiry == null || user.RefreshTokenExpiry < DateTime.UtcNow)
                throw new UnauthorizedAccessException("Refresh token has expired. Please log in again.");

            return await BuildAuthResponseAsync(user);
        }

        //Logout
        public async Task LogoutAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;

            await _userManager.UpdateAsync(user);
        }

        public async Task RevokeAllTokensAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            //Updates security stamp, which immediately invalidates all currently active JWTs for this user
            await _userManager.UpdateSecurityStampAsync(user);

            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;

            await _userManager.UpdateAsync(user);
        }

        //Change password
        public async Task ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId) ?? throw new ArgumentException("User not found.");

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

            if (!result.Succeeded) throw new ArgumentException(result.Errors.First().Description);
        }

        //Forgot password
        public async Task<bool> ForgotPasswordAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required");

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = await _userManager.FindByEmailAsync(normalizedEmail);


            if (user != null && !user.IsDeleted)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var resetLink = $"{_configuration["App:BaseUrl"]}/reset-password?email={Uri.EscapeDataString(normalizedEmail)}&token={Uri.EscapeDataString(token)}";

                await _emailService.SendEmailAsync(
                    normalizedEmail,
                    "Reset your RentIt password",
                    "<h2>Password Reset</h2>" +
                    "<p>We received a request to reset your password.</p>" +
                    $"<p><a href='{resetLink}'>Reset Password</a></p>" +
                    "<p>This link expires in 1 hour.</p>"
                );
            }

            // Always return true (prevents email enumeration)
            return true;
        }

        //Reset password
        public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email.Trim().ToLower());

            if (user == null)
                return false;

            var result = await _userManager.ResetPasswordAsync(
                user,
                dto.Token,
                dto.NewPassword
            );

            if (!result.Succeeded)
                return false;

            //Important: invalidate old sessions/tokens
            await _userManager.UpdateSecurityStampAsync(user);

            return true;
        }
        public async Task ResendConfirmationEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email.Trim().ToLower());

            if (user == null || user.IsDeleted || await _userManager.IsEmailConfirmedAsync(user)) return;

            var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmLink = $"{_configuration["App:FrontendUrl"]}/auth/confirm?userId={user.Id}&token={Uri.EscapeDataString(confirmToken)}";
            
            await _emailService.SendEmailAsync(
                user.Email!,
                "Confirm your RentIt account (resent)",
                $"<h2>Email Confirmation</h2>" +
                $"<p>Here is your new confirmation link for your RentIt account:</p>" +
                $"<p><a href='{confirmLink}'>Confirm Email</a></p>" +
                $"<p>If you did not request this, you can ignore this email.</p>"
            );
        }

        public async Task<bool> IsEmailTakenAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email.Trim().ToLower());
            return user != null;
        }

        public async Task<bool> IsUsernameTakenAsync(string username)
        {
            var user = await _userManager.FindByNameAsync(username.Trim());
            return user != null;
        }

        private async Task<AuthResponseDto> BuildAuthResponseAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "User";

            var jwt = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiresAt = DateTime.UtcNow.AddMinutes(double.Parse(jwt["ExpiryMinutes"]!));

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, role),
                new Claim("username", user.UserName ?? string.Empty),
                new Claim("avatarUrl", user.AvatarUrl ?? string.Empty),
                new Claim("score", user.Score.ToString()),
                new Claim("isVerified", user.IsVerified.ToString().ToLower()),
                new Claim("securityStamp", user.SecurityStamp ?? string.Empty)
            };

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds
            );

            //Rotate refresh token on every use — old token immediately invalidated
            var rawRefreshToken = GenerateRefreshToken();
            user.RefreshToken = HashToken(rawRefreshToken);
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new InvalidOperationException("Failed to persist refresh token. Please try again.");

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = rawRefreshToken,
                UserId = user.Id,
                FullName = user.FullName,
                Username = user.UserName!,
                Email = user.Email!,
                Role = role,
                AvatarUrl = user.AvatarUrl,
                Score = user.Score,
                IsVerified = user.IsVerified,
                ExpiresAt = expiresAt
            };
        }

        //Generates a cryptographically random 64-byte base64 string
        private static string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }
    }
}