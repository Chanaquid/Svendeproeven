using backend.Controllers;
using backend.Dtos;
using backend.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace backend.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _service = new(MockBehavior.Strict);
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _sut = new AuthController(_service.Object);
    }

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
        body.Data!.UserId.Should().Be("user-123");
    }

    [Fact]
    public async Task ConfirmEmail_WhenTrue_ReturnsOk()
    {
        _service.Setup(s => s.ConfirmEmailAsync("user-1", "tok")).ReturnsAsync(true);

        var result = await _sut.ConfirmEmail("user-1", "tok");

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ConfirmEmail_WhenFalse_ReturnsBadRequest()
    {
        _service.Setup(s => s.ConfirmEmailAsync("user-1", "bad")).ReturnsAsync(false);

        var result = await _sut.ConfirmEmail("user-1", "bad");

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Login_ReturnsTokenEnvelope()
    {
        var input = new LoginRequestDto { Email = "a@a.com", Password = "pw" };

        var response = new AuthResponseDto
        {
            Token = "jwt",
            RefreshToken = "rt",
            UserId = "u1",
            Email = "a@a.com",
            Role = "User"
        };

        _service.Setup(s => s.LoginAsync(input)).ReturnsAsync(response);

        var result = await _sut.Login(input);

        var body = (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<AuthResponseDto>>().Subject;

        body.Data!.Token.Should().Be("jwt");
    }

    [Fact]
    public async Task Refresh_ReturnsOk()
    {
        var input = new RefreshTokenRequestDto { RefreshToken = "old" };
        var response = new AuthResponseDto { Token = "new", RefreshToken = "newrt" };

        _service.Setup(s => s.RefreshTokenAsync(input)).ReturnsAsync(response);

        var result = await _sut.Refresh(input);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Logout_UsesUserId()
    {
        _service.Setup(s => s.LogoutAsync("user-99")).Returns(Task.CompletedTask);

        ControllerTestHelper.SetUser(_sut, "user-99");

        var result = await _sut.Logout();

        result.Result.Should().BeOfType<OkObjectResult>();
        _service.Verify(s => s.LogoutAsync("user-99"), Times.Once);
    }

    [Fact]
    public async Task Logout_WithoutUser_Throws()
    {
        var act = async () => await _sut.Logout();
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task ChangePassword_UsesUserId()
    {
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "old",
            NewPassword = "new1!",
            ConfirmNewPassword = "new1!"
        };

        _service.Setup(s => s.ChangePasswordAsync("user-1", dto)).Returns(Task.CompletedTask);

        ControllerTestHelper.SetUser(_sut, "user-1");

        var result = await _sut.ChangePassword(dto);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ForgotPassword_AlwaysOk()
    {
        var dto = new ForgotPasswordDto { Email = "x@x.com" };

        _service.Setup(s => s.ForgotPasswordAsync(dto.Email)).ReturnsAsync(false);

        var result = await _sut.ForgotPassword(dto);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ResetPassword_Success_ReturnsOk()
    {
        var dto = new ResetPasswordDto
        {
            Email = "a@b.com",
            Token = "t",
            NewPassword = "A1b2c3!",
            ConfirmNewPassword = "A1b2c3!"
        };

        _service.Setup(s => s.ResetPasswordAsync(dto)).ReturnsAsync(true);

        var result = await _sut.ResetPassword(dto);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ResetPassword_Fail_ReturnsBadRequest()
    {
        var dto = new ResetPasswordDto
        {
            Email = "a@b.com",
            Token = "bad",
            NewPassword = "A1b2c3!",
            ConfirmNewPassword = "A1b2c3!"
        };

        _service.Setup(s => s.ResetPasswordAsync(dto)).ReturnsAsync(false);

        var result = await _sut.ResetPassword(dto);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Theory]
    [InlineData(true, "Email is taken.")]
    [InlineData(false, "Email is available.")]
    public async Task CheckEmail(bool taken, string message)
    {
        _service.Setup(s => s.IsEmailTakenAsync("x@x.com")).ReturnsAsync(taken);

        var result = await _sut.CheckEmail("x@x.com");

        var body = (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<bool>>().Subject;

        body.Message.Should().Be(message);
    }

    [Theory]
    [InlineData(true, "Username is taken.")]
    [InlineData(false, "Username is available.")]
    public async Task CheckUsername(bool taken, string message)
    {
        _service.Setup(s => s.IsUsernameTakenAsync("alice")).ReturnsAsync(taken);

        var result = await _sut.CheckUsername("alice");

        var body = (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<bool>>().Subject;

        body.Message.Should().Be(message);
    }

    [Fact]
    public async Task RevokeAll_UsesUserId()
    {
        _service.Setup(s => s.RevokeAllTokensAsync("user-55")).Returns(Task.CompletedTask);

        ControllerTestHelper.SetUser(_sut, "user-55");

        var result = await _sut.RevokeAll();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ResendConfirmation_CallsService()
    {
        var dto = new ResendConfirmationDto { Email = "test@test.com" };

        _service.Setup(s => s.ResendConfirmationEmailAsync(dto.Email)).Returns(Task.CompletedTask);

        var result = await _sut.ResendConfirmation(dto);

        result.Result.Should().BeOfType<OkObjectResult>();
        _service.Verify();
    }
}