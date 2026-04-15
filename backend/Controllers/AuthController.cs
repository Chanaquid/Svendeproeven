using backend.Dtos;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<RegisterUserResponseDto>>> Register([FromBody] RegisterUserRequestDto dto)
        {
        
            var result = await _authService.RegisterAsync(dto);
            return Ok(ApiResponse<RegisterUserResponseDto>.Ok(result, "Registration successful. Please check your email to confirm your account."));
  
        }

        [HttpGet("confirm-email")]
        public async Task<ActionResult<ApiResponse<string>>> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
        {
            var success = await _authService.ConfirmEmailAsync(userId, token);

            if (!success)
                return BadRequest(ApiResponse<string>.Fail("Email confirmation failed. The link may have expired."));

            return Ok(ApiResponse<string>.Ok(string.Empty, "Email confirmed. You can now log in."));
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginRequestDto dto)
        {
   
            var result = await _authService.LoginAsync(dto);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Login successful."));
            
  
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Refresh([FromBody] RefreshTokenRequestDto dto)
        {
            var result = await _authService.RefreshTokenAsync(dto);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Token refreshed successfully."));
  
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<ActionResult<ApiResponse<string>>> Logout()
        {
            await _authService.LogoutAsync(Caller.UserId);
            return Ok(ApiResponse<string>.Ok(string.Empty, "Logged out successfully."));
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<ActionResult<ApiResponse<string>>> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            await _authService.ChangePasswordAsync(Caller.UserId, dto);

            return Ok(ApiResponse<string>.Ok("", "Password changed successfully."));
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult<ApiResponse<string>>> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            //Always returns 200 — so it doesnt reveal whether the email exists
            await _authService.ForgotPasswordAsync(dto.Email);
            return Ok(ApiResponse<string>.Ok(string.Empty, "If an account with that email exists, a reset link has been sent."));
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult<ApiResponse<string>>> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var success = await _authService.ResetPasswordAsync(dto);

            if (!success)
                return BadRequest(ApiResponse<string>.Fail("Password reset failed. The link may have expired."));

            return Ok(ApiResponse<string>.Ok(string.Empty, "Password reset successfully. You can now log in."));
        }

        [HttpPost("resend-confirmation")]
        public async Task<ActionResult<ApiResponse<string>>> ResendConfirmation([FromBody] ResendConfirmationDto dto)
        {
            await _authService.ResendConfirmationEmailAsync(dto.Email);
            return Ok(ApiResponse<string>.Ok(string.Empty, "If your email is registered and unconfirmed, a new confirmation link has been sent."));
        }

        [HttpGet("check-email")]
        public async Task<ActionResult<ApiResponse<bool>>> CheckEmail([FromQuery] string email)
        {
            var isTaken = await _authService.IsEmailTakenAsync(email);
            return Ok(ApiResponse<bool>.Ok(isTaken, isTaken ? "Email is taken." : "Email is available."));
        }

        [HttpGet("check-username")]
        public async Task<ActionResult<ApiResponse<bool>>> CheckUsername([FromQuery] string username)
        {
            var isTaken = await _authService.IsUsernameTakenAsync(username);
            return Ok(ApiResponse<bool>.Ok(isTaken, isTaken ? "Username is taken." : "Username is available."));
        }

        [Authorize]
        [HttpPost("revoke-all")]
        public async Task<ActionResult<ApiResponse<string>>> RevokeAll()
        {
            await _authService.RevokeAllTokensAsync(Caller.UserId);
            return Ok(ApiResponse<string>.Ok(string.Empty, "All sessions revoked."));
        }


    }
}