using backend.Dtos;

namespace backend.Interfaces
{
    public interface IAuthService
    {
        //Auth
        Task<RegisterUserResponseDto> RegisterAsync(RegisterUserRequestDto dto);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto);
        Task LogoutAsync(string userId);
        Task RevokeAllTokensAsync(string userId); //Admin force logout

        //Password
        Task ChangePasswordAsync(string userId, ChangePasswordDto dto);
        Task<bool> ForgotPasswordAsync(string email);
        Task<bool> ResetPasswordAsync(ResetPasswordDto dto);

        //Email
        Task<bool> ConfirmEmailAsync(string userId, string token);
        Task ResendConfirmationEmailAsync(string email);

        //Validation
        Task<bool> IsEmailTakenAsync(string email);
        Task<bool> IsUsernameTakenAsync(string username);
    }
}