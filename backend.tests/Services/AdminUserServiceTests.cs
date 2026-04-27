using backend.Common;
using backend.Dtos;
using backend.Helpers;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;

namespace backend.Tests.Services;

public class AdminUserServiceTests
{
    private readonly Mock<IAdminUserRepository> _adminRepo = new();
    private readonly Mock<IItemRepository> _itemRepo = new();
    private readonly Mock<ILoanRepository> _loanRepo = new();
    private readonly Mock<IFineRepository> _fineRepo = new();
    private readonly Mock<IDisputeRepository> _disputeRepo = new();
    private readonly Mock<INotificationService> _notification = new();
    private readonly Mock<IScoreHistoryRepository> _scoreRepo = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<IConfiguration> _config = new();

    private readonly Mock<UserManager<ApplicationUser>> _userManager;

    private readonly AdminUserService _sut;

    public AdminUserServiceTests()
    {
        _userManager = MockUserManager();

        _sut = new AdminUserService(
            _adminRepo.Object,
            _userManager.Object,
            _email.Object,
            _config.Object,
            _itemRepo.Object,
            _loanRepo.Object,
            _fineRepo.Object,
            _disputeRepo.Object,
            _notification.Object,
            _scoreRepo.Object
        );
    }


    [Fact]
    public async Task GetUserById_WhenUserExists_ReturnsDto()
    {
        var user = new ApplicationUser { Id = "1", UserName = "john", Email = "a@a.com" };

        _adminRepo.Setup(x => x.GetUserByIdAsync("1")).ReturnsAsync(user);
        _userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { Roles.User });
        _fineRepo.Setup(x => x.GetOutstandingTotalByUserAsync("1")).ReturnsAsync(10);

        var result = await _sut.GetUserByIdAsync("1");

        result.Username.Should().Be("john");
        result.UnpaidFinesTotal.Should().Be(10);
    }

    [Fact]
    public async Task GetUserById_WhenMissing_Throws()
    {
        _adminRepo.Setup(x => x.GetUserByIdAsync("1")).ReturnsAsync((ApplicationUser?)null);

        Func<Task> act = async () => await _sut.GetUserByIdAsync("1");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }


    [Fact]
    public async Task GetAllBannedUsers_ReturnsMappedPagedResult()
    {
        var user = new ApplicationUser { Id = "1", UserName = "u1" };

        _adminRepo
    .Setup(x => x.GetAllBannedUsersAsync(
        It.IsAny<UserFilter>(),
        It.IsAny<PagedRequest>(),
        false))
    .ReturnsAsync(new PagedResult<UserWithRole>
    {
        Items = new List<UserWithRole>
        {
            new UserWithRole
            {
                User = new ApplicationUser { Id = "1", UserName = "u1" },
                Role = Roles.User
            }
        },
        TotalCount = 1,
        Page = 1,
        PageSize = 10
    });

        _fineRepo.Setup(x => x.GetOutstandingTotalsByUsersAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(new Dictionary<string, decimal> { ["1"] = 50 });

        var result = await _sut.GetAllBannedUsersAsync(null, new PagedRequest());

        result.Items.Should().HaveCount(1);
        result.Items.First().UnpaidFinesTotal.Should().Be(50);
    }


    [Fact]
    public async Task SoftDelete_WhenValid_DeletesUser()
    {
        var user = new ApplicationUser { Id = "1" };

        _adminRepo.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(f => f());

        _adminRepo.Setup(x => x.GetUserByIdAsync("1")).ReturnsAsync(user);
        _loanRepo.Setup(x => x.HasOngoingLoansAsBorrower("1")).ReturnsAsync(false);
        _loanRepo.Setup(x => x.HasOngoingLoansAsOwner("1")).ReturnsAsync(false);
        _disputeRepo.Setup(x => x.HasActiveDisputeByUserIdAsync("1")).ReturnsAsync(false);
        _itemRepo.Setup(x => x.GetByOwnerIdAsync("1")).ReturnsAsync(new List<Item>());

        var result = await _sut.AdminSoftDeleteUserAsync("1", "admin");

        result.Success.Should().BeTrue();
    }


    [Fact]
    public async Task BanUser_SetsBanAndSendsNotification()
    {
        var user = new ApplicationUser { Id = "1", Email = "a@a.com" };

        _adminRepo.Setup(x => x.GetUserByIdAsync("1")).ReturnsAsync(user);
        _userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { Roles.User });

        var dto = new BanUserDto { Reason = "spam" };

        await _sut.BanUserAsync("1", "admin", dto);

        _notification.Verify(x => x.SendAsync(
            "1",
            NotificationType.SystemAlert,
            It.IsAny<string>(),
            null,
            NotificationReferenceType.SystemAlert
        ), Times.Once);
    }

    [Fact]
    public async Task BanUser_WhenAdminTriesToBanAdmin_Throws()
    {
        var user = new ApplicationUser { Id = "1" };

        _adminRepo.Setup(x => x.GetUserByIdAsync("1")).ReturnsAsync(user);
        _userManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { Roles.Admin });

        var dto = new BanUserDto { Reason = "test" };

        Func<Task> act = async () => await _sut.BanUserAsync("1", "admin", dto);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }


    [Fact]
    public async Task UnbanUser_WhenNotBanned_Throws()
    {
        var user = new ApplicationUser { Id = "1", IsBanned = false };

        _adminRepo.Setup(x => x.GetUserByIdAsync("1")).ReturnsAsync(user);

        var dto = new UnbanUserDto { Reason = "fix" };

        Func<Task> act = async () => await _sut.UnbanUserAsync("1", "admin", dto);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }


    private static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!
        );
    }
}