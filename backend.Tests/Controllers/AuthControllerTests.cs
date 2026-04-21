using backend.Controllers;
using backend.Dtos;
using backend.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace backend.Tests.Controllers;

// AuthController teaches TWO new patterns beyond the simple CategoryController:
//
//   1. Endpoints that return BadRequest on failure (ConfirmEmail, ResetPassword).
//      The controller reads the bool from the service and picks Ok or BadRequest —
//      so we need a test for EACH branch.
//
//   2. [Authorize] endpoints that use Caller.UserId (Logout, ChangePassword, RevokeAll).
//      These crash with UnauthorizedAppException if we forget to set claims — so
//      we use ControllerTestHelper.SetUser to simulate a logged-in caller.
public class AuthControllerTests
{
    private readonly Mock<IAuthService> _service = new(MockBehavior.Strict);
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _sut = new AuthController(_service.Object);
    }

    // ---------- POST /api/auth/register ----------

    [Fact]
    public async Task Register_ReturnsOk_WithRegistrationMessage()
    {
        var input = new RegisterUserRequestDto
        {
            FullName = "Alice Example",
            Email = "alice@example.com",
            Username = "alice",
            Password = "P@ssw0rd!",
            ConfirmPassword = "P@ssw0rd!",
            Address = "Main St 1",
            DateOfBirth = new DateTime(2000, 1, 1)
        };
        var response = new RegisterUserResponseDto
        {
            UserId = "user-123",
            Email = "alice@example.com",
            Username = "alice"
        };

        _service.Setup(s => s.RegisterAsync(input)).ReturnsAsync(response);

        var result = await _sut.Register(input);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value.Should().BeOfType<ApiResponse<RegisterUserResponseDto>>().Subject;

        body.Success.Should().BeTrue();
        body.Message.Should().StartWith("Registration successful");
        body.Data!.UserId.Should().Be("user-123");
    }

    // ---------- GET /api/auth/confirm-email ----------
    // Two branches: success returns Ok, failure returns BadRequest.

    [Fact]
    public async Task ConfirmEmail_WhenServiceReturnsTrue_ReturnsOk()
    {
        _service.Setup(s => s.ConfirmEmailAsync("user-1", "tok")).ReturnsAsync(true);

        var result = await _sut.ConfirmEmail("user-1", "tok");

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value.Should().BeOfType<ApiResponse<string>>().Subject;
        body.Success.Should().BeTrue();
        body.Message.Should().Contain("Email confirmed");
    }

    [Fact]
    public async Task ConfirmEmail_WhenServiceReturnsFalse_ReturnsBadRequest()
    {
        _service.Setup(s => s.ConfirmEmailAsync("user-1", "bad-token")).ReturnsAsync(false);

        var result = await _sut.ConfirmEmail("user-1", "bad-token");

        // This is the key difference — BadRequestObjectResult, not OkObjectResult
        var bad = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var body = bad.Value.Should().BeOfType<ApiResponse<string>>().Subject;
        body.Success.Should().BeFalse();
        body.Message.Should().Contain("Email confirmation failed");
    }

    // ---------- POST /api/auth/login ----------

    [Fact]
    public async Task Login_ReturnsTokenEnvelope()
    {
        var input = new LoginRequestDto { Email = "alice@example.com", Password = "pw" };
        var response = new AuthResponseDto
        {
            Token = "jwt",
            RefreshToken = "rt",
            UserId = "user-1",
            Email = "alice@example.com",
            Role = "User"
        };

        _service.Setup(s => s.LoginAsync(input)).ReturnsAsync(response);

        var result = await _sut.Login(input);

        var body = (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<AuthResponseDto>>().Subject;
        body.Data!.Token.Should().Be("jwt");
        body.Message.Should().Be("Login successful.");
    }

    // ---------- POST /api/auth/refresh ----------

    [Fact]
    public async Task Refresh_PassesDtoThrough_AndReturnsOk()
    {
        var input = new RefreshTokenRequestDto { RefreshToken = "old-rt" };
        var response = new AuthResponseDto { Token = "new-jwt", RefreshToken = "new-rt" };

        _service.Setup(s => s.RefreshTokenAsync(input)).ReturnsAsync(response);

        var result = await _sut.Refresh(input);

        var body = (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<AuthResponseDto>>().Subject;
        body.Data!.RefreshToken.Should().Be("new-rt");
    }

    // ---------- POST /api/auth/logout  ([Authorize], uses Caller.UserId) ----------

    [Fact]
    public async Task Logout_UsesCallerUserId()
    {
        _service.Setup(s => s.LogoutAsync("user-99")).Returns(Task.CompletedTask);

        // Setting claims is REQUIRED for any endpoint that touches Caller.UserId;
        // without this the base controller throws UnauthorizedAppException.
        ControllerTestHelper.SetUser(_sut, userId: "user-99");

        var result = await _sut.Logout();

        (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<string>>()
            .Which.Message.Should().Be("Logged out successfully.");

        _service.Verify(s => s.LogoutAsync("user-99"), Times.Once);
    }

    [Fact]
    public async Task Logout_WithoutAuthenticatedUser_Throws()
    {
        // No ControllerContext set at all -> HttpContext.User is null -> NRE
        // when BaseController.BuildCallerContext tries to read claims.
        // In production this can't happen because [Authorize] guarantees an
        // authenticated principal before the action runs; the unit test merely
        // documents what happens if someone bypasses the framework.
        var act = async () => await _sut.Logout();

        await act.Should().ThrowAsync<Exception>();
    }

    // ---------- POST /api/auth/change-password ----------

    [Fact]
    public async Task ChangePassword_PassesUserIdAndDto()
    {
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "old",
            NewPassword = "newPass1!",
            ConfirmNewPassword = "newPass1!"
        };
        _service.Setup(s => s.ChangePasswordAsync("user-77", dto)).Returns(Task.CompletedTask);

        ControllerTestHelper.SetUser(_sut, userId: "user-77");

        var result = await _sut.ChangePassword(dto);

        (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<string>>()
            .Which.Message.Should().Be("Password changed successfully.");
    }

    // ---------- POST /api/auth/forgot-password (always 200) ----------

    [Fact]
    public async Task ForgotPassword_AlwaysReturnsOk_EvenIfEmailUnknown()
    {
        // Your controller comment says this is intentional: don't reveal whether
        // the email exists. So regardless of the service's bool, we get Ok.
        var dto = new ForgotPasswordDto { Email = "nobody@example.com" };
        _service.Setup(s => s.ForgotPasswordAsync("nobody@example.com")).ReturnsAsync(false);

        var result = await _sut.ForgotPassword(dto);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    // ---------- POST /api/auth/reset-password (two branches) ----------

    [Fact]
    public async Task ResetPassword_WhenSuccessful_ReturnsOk()
    {
        var dto = new ResetPasswordDto
        {
            Email = "a@b.com",
            Token = "t",
            NewPassword = "Abcdef12!",
            ConfirmNewPassword = "Abcdef12!"
        };
        _service.Setup(s => s.ResetPasswordAsync(dto)).ReturnsAsync(true);

        var result = await _sut.ResetPassword(dto);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ResetPassword_WhenFails_ReturnsBadRequest()
    {
        var dto = new ResetPasswordDto
        {
            Email = "a@b.com",
            Token = "expired",
            NewPassword = "Abcdef12!",
            ConfirmNewPassword = "Abcdef12!"
        };
        _service.Setup(s => s.ResetPasswordAsync(dto)).ReturnsAsync(false);

        var result = await _sut.ResetPassword(dto);

        var bad = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        (bad.Value as ApiResponse<string>)!.Success.Should().BeFalse();
    }

    // ---------- GET /api/auth/check-email and /check-username ----------
    // These use a conditional message based on the boolean result — good to test.

    [Theory]
    [InlineData(true, "Email is taken.")]
    [InlineData(false, "Email is available.")]
    public async Task CheckEmail_MessageDependsOnResult(bool isTaken, string expectedMessage)
    {
        _service.Setup(s => s.IsEmailTakenAsync("x@y.com")).ReturnsAsync(isTaken);

        var result = await _sut.CheckEmail("x@y.com");

        var body = (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<bool>>().Subject;
        body.Data.Should().Be(isTaken);
        body.Message.Should().Be(expectedMessage);
    }

    [Theory]
    [InlineData(true, "Username is taken.")]
    [InlineData(false, "Username is available.")]
    public async Task CheckUsername_MessageDependsOnResult(bool isTaken, string expectedMessage)
    {
        _service.Setup(s => s.IsUsernameTakenAsync("alice")).ReturnsAsync(isTaken);

        var result = await _sut.CheckUsername("alice");

        var body = (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<bool>>().Subject;
        body.Message.Should().Be(expectedMessage);
    }

    // ---------- POST /api/auth/revoke-all ----------

    [Fact]
    public async Task RevokeAll_UsesCallerUserId()
    {
        _service.Setup(s => s.RevokeAllTokensAsync("user-55")).Returns(Task.CompletedTask);
        ControllerTestHelper.SetUser(_sut, userId: "user-55");

        var result = await _sut.RevokeAll();

        (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<string>>()
            .Which.Message.Should().Be("All sessions revoked.");
    }

    // ---------- POST /api/auth/resend-confirmation ----------

    [Fact]
    public async Task ResendConfirmation_PassesEmail_AndReturnsGenericMessage()
    {
        var dto = new ResendConfirmationDto { Email = "test@example.com" };
        _service.Setup(s => s.ResendConfirmationEmailAsync("test@example.com"))
                .Returns(Task.CompletedTask);

        var result = await _sut.ResendConfirmation(dto);

        result.Result.Should().BeOfType<OkObjectResult>();
        _service.Verify(s => s.ResendConfirmationEmailAsync("test@example.com"), Times.Once);
    }
}