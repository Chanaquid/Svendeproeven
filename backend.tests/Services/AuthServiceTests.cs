using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace backend.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);

        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            null, null, null, null);

        _emailServiceMock = new Mock<IEmailService>();
        _configurationMock = new Mock<IConfiguration>();

        //Wire up config sections used in BuildAuthResponseAsync
        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(s => s["Key"]).Returns("supersecretkey_that_is_long_enough_32chars!!");
        jwtSection.Setup(s => s["Issuer"]).Returns("TestIssuer");
        jwtSection.Setup(s => s["Audience"]).Returns("TestAudience");
        jwtSection.Setup(s => s["ExpiryMinutes"]).Returns("60");
        _configurationMock.Setup(c => c.GetSection("Jwt")).Returns(jwtSection.Object);
        _configurationMock.Setup(c => c["App:FrontendUrl"]).Returns("https://app.test");
        _configurationMock.Setup(c => c["App:BaseUrl"]).Returns("https://app.test");

        _sut = new AuthService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _emailServiceMock.Object,
            _configurationMock.Object);
    }


    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ThrowsArgumentException()
    {
        var existing = new ApplicationUser { Email = "test@test.com", IsDeleted = false };
        _userManagerMock.Setup(m => m.FindByEmailAsync("test@test.com")).ReturnsAsync(existing);

        var dto = new RegisterUserRequestDto
        {
            FullName = "Jane",
            Email = "test@test.com",
            Username = "jane",
            Password = "Pass123!",
            Address = "123 St",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(dto));
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailBelongsToDeletedAccount_ThrowsWithSupportMessage()
    {
        var deleted = new ApplicationUser { Email = "ghost@test.com", IsDeleted = true };
        _userManagerMock.Setup(m => m.FindByEmailAsync("ghost@test.com")).ReturnsAsync(deleted);

        var dto = new RegisterUserRequestDto
        {
            FullName = "Ghost",
            Email = "ghost@test.com",
            Username = "ghost",
            Password = "Pass123!",
            Address = "123 St",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(dto));
        Assert.Contains("contact support", ex.Message);
    }

    [Fact]
    public async Task RegisterAsync_WhenUsernameTaken_ThrowsArgumentException()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(m => m.FindByNameAsync("taken")).ReturnsAsync(new ApplicationUser());

        var dto = new RegisterUserRequestDto
        {
            FullName = "Jane",
            Email = "new@test.com",
            Username = "taken",
            Password = "Pass123!",
            Address = "123 St",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(dto));
        Assert.Contains("Username", ex.Message);
    }

    [Fact]
    public async Task RegisterAsync_WhenIdentityCreateFails_ThrowsWithErrors()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak." }));

        var dto = new RegisterUserRequestDto
        {
            FullName = "Jane",
            Email = "new@test.com",
            Username = "jane",
            Password = "weak",
            Address = "123 St",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync(dto));
        Assert.Contains("Password too weak", ex.Message);
    }

    [Fact]
    public async Task RegisterAsync_Success_ReturnsUsernameAndEmail_AndSendsConfirmationEmail()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync("confirm-token");

        var dto = new RegisterUserRequestDto
        {
            FullName = "Jane Doe",
            Email = "jane@test.com",
            Username = "janedoe",
            Password = "Pass123!",
            Address = "123 St",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        var result = await _sut.RegisterAsync(dto);

        Assert.Equal("janedoe", result.Username);
        Assert.Equal("jane@test.com", result.Email);
        Assert.NotEmpty(result.UserId);
        Assert.NotEmpty(result.Message);

        _emailServiceMock.Verify(
            e => e.SendEmailAsync("jane@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }


    [Fact]
    public async Task ConfirmEmailAsync_WhenUserNotFound_ReturnsFalse()
    {
        _userManagerMock.Setup(m => m.FindByIdAsync("bad-id")).ReturnsAsync((ApplicationUser?)null);
        var result = await _sut.ConfirmEmailAsync("bad-id", "token");
        Assert.False(result);
    }

    [Fact]
    public async Task ConfirmEmailAsync_WhenTokenValid_ReturnsTrue()
    {
        var user = new ApplicationUser { Id = "uid1" };
        _userManagerMock.Setup(m => m.FindByIdAsync("uid1")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.ConfirmEmailAsync(user, "good-token"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _sut.ConfirmEmailAsync("uid1", "good-token");
        Assert.True(result);
    }


    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ThrowsArgumentException()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.LoginAsync(new LoginRequestDto { Email = "no@one.com", Password = "x" }));
    }

    [Fact]
    public async Task LoginAsync_WhenUserDeleted_ThrowsArgumentException()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new ApplicationUser { IsDeleted = true });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.LoginAsync(new LoginRequestDto { Email = "del@test.com", Password = "x" }));
    }

    [Fact]
    public async Task LoginAsync_WhenPermaBanned_ThrowsWithPermBanPrefix()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new ApplicationUser { IsBanned = true, BanReason = "fraud" });

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.LoginAsync(new LoginRequestDto { Email = "ban@test.com", Password = "x" }));
        Assert.StartsWith("PERM_BAN", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_WhenTempBanned_ThrowsWithTempBanPrefix()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new ApplicationUser
            {
                IsBanned = true,
                BanReason = "abuse",
                BanExpiresAt = DateTime.UtcNow.AddDays(1)
            });

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.LoginAsync(new LoginRequestDto { Email = "ban@test.com", Password = "x" }));
        Assert.StartsWith("TEMP_BAN", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_WhenEmailNotConfirmed_ThrowsArgumentException()
    {
        var user = new ApplicationUser { IsBanned = false, IsDeleted = false };
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(false);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.LoginAsync(new LoginRequestDto { Email = "u@test.com", Password = "x" }));
    }

    [Fact]
    public async Task LoginAsync_WhenLockedOut_ThrowsArgumentException()
    {
        var user = new ApplicationUser { IsBanned = false, IsDeleted = false };
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
        _signInManagerMock.Setup(m => m.CheckPasswordSignInAsync(user, It.IsAny<string>(), true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.LoginAsync(new LoginRequestDto { Email = "u@test.com", Password = "wrong" }));
        Assert.Contains("locked", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_WhenWrongPassword_ThrowsArgumentException()
    {
        var user = new ApplicationUser { IsBanned = false, IsDeleted = false };
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
        _signInManagerMock.Setup(m => m.CheckPasswordSignInAsync(user, It.IsAny<string>(), true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.LoginAsync(new LoginRequestDto { Email = "u@test.com", Password = "wrong" }));
    }


    [Fact]
    public async Task LogoutAsync_WhenUserExists_ClearsRefreshToken()
    {
        var user = new ApplicationUser { Id = "u1", RefreshToken = "hash", RefreshTokenExpiry = DateTime.UtcNow.AddDays(1) };
        _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        await _sut.LogoutAsync("u1");

        Assert.Null(user.RefreshToken);
        Assert.Null(user.RefreshTokenExpiry);
    }

    [Fact]
    public async Task LogoutAsync_WhenUserNotFound_DoesNotThrow()
    {
        _userManagerMock.Setup(m => m.FindByIdAsync("ghost")).ReturnsAsync((ApplicationUser?)null);
        await _sut.LogoutAsync("ghost"); //should not throw
    }


    [Fact]
    public async Task ChangePasswordAsync_WhenUserNotFound_ThrowsArgumentException()
    {
        _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.ChangePasswordAsync("ghost", new ChangePasswordDto { CurrentPassword = "old", NewPassword = "new" }));
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenOldPasswordWrong_ThrowsArgumentException()
    {
        var user = new ApplicationUser();
        _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.ChangePasswordAsync(user, "wrong", "new"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Incorrect password." }));

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.ChangePasswordAsync("u1", new ChangePasswordDto { CurrentPassword = "wrong", NewPassword = "new" }));
        Assert.Contains("Incorrect", ex.Message);
    }


    [Fact]
    public async Task IsEmailTakenAsync_WhenEmailExists_ReturnsTrue()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync("taken@test.com")).ReturnsAsync(new ApplicationUser());
        Assert.True(await _sut.IsEmailTakenAsync("taken@test.com"));
    }

    [Fact]
    public async Task IsEmailTakenAsync_WhenEmailFree_ReturnsFalse()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync("free@test.com")).ReturnsAsync((ApplicationUser?)null);
        Assert.False(await _sut.IsEmailTakenAsync("free@test.com"));
    }

    [Fact]
    public async Task IsUsernameTakenAsync_WhenTaken_ReturnsTrue()
    {
        _userManagerMock.Setup(m => m.FindByNameAsync("taken")).ReturnsAsync(new ApplicationUser());
        Assert.True(await _sut.IsUsernameTakenAsync("taken"));
    }

    [Fact]
    public async Task IsUsernameTakenAsync_WhenFree_ReturnsFalse()
    {
        _userManagerMock.Setup(m => m.FindByNameAsync("free")).ReturnsAsync((ApplicationUser?)null);
        Assert.False(await _sut.IsUsernameTakenAsync("free"));
    }

    //ForgotPasswordAsync

    [Fact]
    public async Task ForgotPasswordAsync_AlwaysReturnsTrue_EvenIfUserNotFound()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
        Assert.True(await _sut.ForgotPasswordAsync("ghost@test.com"));
        _emailServiceMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WhenUserExists_SendsResetEmail()
    {
        var user = new ApplicationUser { Email = "real@test.com", IsDeleted = false };
        _userManagerMock.Setup(m => m.FindByEmailAsync("real@test.com")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");

        Assert.True(await _sut.ForgotPasswordAsync("real@test.com"));
        _emailServiceMock.Verify(e => e.SendEmailAsync("real@test.com", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithEmptyEmail_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.ForgotPasswordAsync(""));
    }
}