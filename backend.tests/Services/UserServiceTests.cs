using backend.Configuration;
using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace backend.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<ILoanRepository> _loanRepositoryMock = new();
        private readonly Mock<IItemRepository> _itemRepositoryMock = new();
        private readonly Mock<IFineRepository> _fineRepositoryMock = new();
        private readonly Mock<IDisputeRepository> _disputeRepositoryMock = new();
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly IConfiguration _configuration;
        private readonly IOptions<ScoreThresholdOptions> _scoreOptions;

        public UserServiceTests()
        {
            _userManagerMock = MockUserManager();

            _scoreOptions = Options.Create(new ScoreThresholdOptions
            {
                BlockedBelow = 20,
                AdminApprovalBelowOrEqual = 50
            });

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["App:BaseUrl"] = "https://localhost:7183"
                })
                .Build();
        }

        private UserService CreateService()
        {
            return new UserService(
                _userRepositoryMock.Object,
                _loanRepositoryMock.Object,
                _itemRepositoryMock.Object,
                _fineRepositoryMock.Object,
                _disputeRepositoryMock.Object,
                _userManagerMock.Object,
                _scoreOptions,
                _emailServiceMock.Object,
                _configuration
            );
        }

        private static Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();

            return new Mock<UserManager<ApplicationUser>>(
                store.Object,
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<IPasswordHasher<ApplicationUser>>(),
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                Mock.Of<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<ApplicationUser>>>()
            );
        }

        private static ApplicationUser CreateUser(
            string id = "user-123",
            int score = 100)
        {
            return new ApplicationUser
            {
                Id = id,
                FullName = "Test User",
                UserName = "testuser",
                Email = "test@test.com",
                PhoneNumber = "12345678",
                Address = "Test Address",
                Gender = "Male",
                Bio = "Bio",
                AvatarUrl = "avatar.png",
                Score = score,
                IsVerified = true,
                IsBanned = false,
                IsDeleted = false,
                MembershipDate = DateTime.UtcNow.AddDays(-10),
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            };
        }

        private static Item CreateItem(int id = 1)
        {
            return new Item
            {
                Id = id,
                OwnerId = "user-123",
                Title = "Hammer",
                Status = ItemStatus.Approved,
                Availability = ItemAvailability.Available,
                IsActive = true
            };
        }

        [Fact]
        public async Task GetProfileAsync_ShouldThrow_WhenUserDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync("missing-user"))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            Func<Task> act = async () => await service.GetProfileAsync("missing-user");

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("User not found.");
        }

        [Fact]
        public async Task GetProfileAsync_ShouldReturnProfile_WhenUserExists()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser(score: 100);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "User" });

            _fineRepositoryMock
                .Setup(r => r.GetOutstandingTotalByUserAsync(user.Id))
                .ReturnsAsync(250);

            _loanRepositoryMock
                .Setup(r => r.GetAllCompletedLoansCountByUserIdAsync(user.Id))
                .ReturnsAsync(3);

            // Act
            var result = await service.GetProfileAsync(user.Id);

            // Assert
            result.Id.Should().Be(user.Id);
            result.FullName.Should().Be(user.FullName);
            result.Username.Should().Be(user.UserName);
            result.Email.Should().Be(user.Email);
            result.Role.Should().Be("User");
            result.Score.Should().Be(100);
            result.UnpaidFinesTotal.Should().Be(250);
            result.TotalCompletedLoans.Should().Be(3);
            result.BorrowingStatus.Should().Be(BorrowingStatus.Free);
        }

        [Fact]
        public async Task GetProfileAsync_ShouldReturnBlockedBorrowingStatus_WhenScoreIsLow()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser(score: 10);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "User" });

            _fineRepositoryMock
                .Setup(r => r.GetOutstandingTotalByUserAsync(user.Id))
                .ReturnsAsync(0);

            _loanRepositoryMock
                .Setup(r => r.GetAllCompletedLoansCountByUserIdAsync(user.Id))
                .ReturnsAsync(0);

            // Act
            var result = await service.GetProfileAsync(user.Id);

            // Assert
            result.BorrowingStatus.Should().Be(BorrowingStatus.Blocked);
        }

        [Fact]
        public async Task GetProfileAsync_ShouldReturnAdminApprovalBorrowingStatus_WhenScoreIsMiddle()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser(score: 50);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "User" });

            _fineRepositoryMock
                .Setup(r => r.GetOutstandingTotalByUserAsync(user.Id))
                .ReturnsAsync(0);

            _loanRepositoryMock
                .Setup(r => r.GetAllCompletedLoansCountByUserIdAsync(user.Id))
                .ReturnsAsync(0);

            // Act
            var result = await service.GetProfileAsync(user.Id);

            // Assert
            result.BorrowingStatus.Should().Be(BorrowingStatus.AdminApproval);
        }

        [Fact]
        public async Task GetPublicProfileAsync_ShouldThrow_WhenProfileDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            _userRepositoryMock
                .Setup(r => r.GetPublicProfileByIdAsync("user-123", "current-user"))
                .ReturnsAsync((UserPublicProfileDto?)null);

            // Act
            Func<Task> act = async () =>
                await service.GetPublicProfileAsync("user-123", "current-user");

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("User not found or is blocked.");
        }


        [Fact]
        public async Task UpdateProfileAsync_ShouldThrow_WhenUserDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var dto = new UpdateProfileDto
            {
                FullName = "New Name"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync("missing-user"))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            Func<Task> act = async () =>
                await service.UpdateProfileAsync("missing-user", dto);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("User not found.");
        }

        [Fact]
        public async Task UpdateProfileAsync_ShouldUpdateSimpleFields()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser();

            var dto = new UpdateProfileDto
            {
                FullName = "New Name",
                Address = "New Address",
                Bio = "New Bio",
                Gender = "Female",
                AvatarUrl = "new-avatar.png"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _userManagerMock
                .Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "User" });

            _fineRepositoryMock
                .Setup(r => r.GetOutstandingTotalByUserAsync(user.Id))
                .ReturnsAsync(0);

            _loanRepositoryMock
                .Setup(r => r.GetAllCompletedLoansCountByUserIdAsync(user.Id))
                .ReturnsAsync(0);

            // Act
            var result = await service.UpdateProfileAsync(user.Id, dto);

            // Assert
            user.FullName.Should().Be("New Name");
            user.Address.Should().Be("New Address");
            user.Bio.Should().Be("New Bio");
            user.Gender.Should().Be("Female");
            user.AvatarUrl.Should().Be("new-avatar.png");

            result.FullName.Should().Be("New Name");

            _userRepositoryMock.Verify(r => r.Update(user), Times.Once);
            _userRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateProfileAsync_ShouldThrow_WhenUsernameIsTaken()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser();
            var otherUser = CreateUser("other-user");

            var dto = new UpdateProfileDto
            {
                UserName = "takenname"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(m => m.FindByNameAsync("takenname"))
                .ReturnsAsync(otherUser);

            // Act
            Func<Task> act = async () =>
                await service.UpdateProfileAsync(user.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Username is already taken.");
        }

        [Fact]
        public async Task UpdateProfileAsync_ShouldThrow_WhenEmailIsTaken()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser();
            var otherUser = CreateUser("other-user");

            var dto = new UpdateProfileDto
            {
                Email = "taken@test.com"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(m => m.FindByEmailAsync("taken@test.com"))
                .ReturnsAsync(otherUser);

            // Act
            Func<Task> act = async () =>
                await service.UpdateProfileAsync(user.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Email is already in use.");
        }

        [Fact]
        public async Task DeleteAccountAsync_ShouldThrow_WhenPasswordIsWrong()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser();

            var dto = new DeleteAccountDto
            {
                Password = "wrong-password"
            };

            _userRepositoryMock
                .Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns<Func<Task>>(action => action());

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(m => m.CheckPasswordAsync(user, dto.Password))
                .ReturnsAsync(false);

            // Act
            Func<Task> act = async () =>
                await service.DeleteAccountAsync(user.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Incorrect password.");
        }

        [Fact]
        public async Task DeleteAccountAsync_ShouldThrow_WhenUserHasOutstandingFines()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser();

            var dto = new DeleteAccountDto
            {
                Password = "password"
            };

            _userRepositoryMock
                .Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns<Func<Task>>(action => action());

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(m => m.CheckPasswordAsync(user, dto.Password))
                .ReturnsAsync(true);

            _fineRepositoryMock
                .Setup(r => r.HasOutstandingFinesAsync(user.Id))
                .ReturnsAsync(true);

            // Act
            Func<Task> act = async () =>
                await service.DeleteAccountAsync(user.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("You have unpaid fines. Please settle them before deleting your account.");
        }

        [Fact]
        public async Task DeleteAccountAsync_ShouldSoftDeleteUser_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser();

            var item1 = CreateItem(1);
            var item2 = CreateItem(2);

            var dto = new DeleteAccountDto
            {
                Password = "password"
            };

            _userRepositoryMock
                .Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns<Func<Task>>(action => action());

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(m => m.CheckPasswordAsync(user, dto.Password))
                .ReturnsAsync(true);

            _fineRepositoryMock
                .Setup(r => r.HasOutstandingFinesAsync(user.Id))
                .ReturnsAsync(false);

            _loanRepositoryMock
                .Setup(r => r.HasOngoingLoansAsBorrower(user.Id))
                .ReturnsAsync(false);

            _loanRepositoryMock
                .Setup(r => r.HasOngoingLoansAsOwner(user.Id))
                .ReturnsAsync(false);

            _disputeRepositoryMock
                .Setup(r => r.HasActiveDisputeByUserIdAsync(user.Id))
                .ReturnsAsync(false);

            _itemRepositoryMock
                .Setup(r => r.GetByOwnerIdAsync(user.Id))
                .ReturnsAsync(new List<Item> { item1, item2 });

            // Act
            await service.DeleteAccountAsync(user.Id, dto);

            // Assert
            user.IsDeleted.Should().BeTrue();
            user.FullName.Should().Be("Deleted User");
            user.Email.Should().Contain("@rentit.local");
            user.IsBanned.Should().BeFalse();
            user.EmailConfirmed.Should().BeFalse();

            item1.IsActive.Should().BeFalse();
            item1.Status.Should().Be(ItemStatus.Deleted);
            item1.Availability.Should().Be(ItemAvailability.Unavailable);

            item2.IsActive.Should().BeFalse();
            item2.Status.Should().Be(ItemStatus.Deleted);
            item2.Availability.Should().Be(ItemAvailability.Unavailable);

            _itemRepositoryMock.Verify(r => r.Update(item1), Times.Once);
            _itemRepositoryMock.Verify(r => r.Update(item2), Times.Once);
            _userRepositoryMock.Verify(r => r.Update(user), Times.Once);
        }

        [Fact]
        public async Task SearchUsersAsync_ShouldReturnPagedUsers()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser(score: 100);

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var filter = new UserFilter();

            _userRepositoryMock
                .Setup(r => r.SearchByUsernameOrEmailAsync(filter, request, "current-user"))
                .ReturnsAsync(new PagedResult<ApplicationUser>
                {
                    Items = new List<ApplicationUser> { user },
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 10
                });

            // Act
            var result = await service.SearchUsersAsync(filter, request, "current-user");

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items[0].Id.Should().Be(user.Id);
            result.Items[0].Username.Should().Be(user.UserName);
            result.Items[0].BorrowingStatus.Should().Be(BorrowingStatus.Free);
            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task EnsureUserCanBorrowAsync_ShouldThrow_WhenUserDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync("missing-user"))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            Func<Task> act = async () =>
                await service.EnsureUserCanBorrowAsync("missing-user");

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("User not found.");
        }

        [Fact]
        public async Task EnsureUserCanBorrowAsync_ShouldThrow_WhenUserIsDeleted()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser();
            user.IsDeleted = true;

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            Func<Task> act = async () =>
                await service.EnsureUserCanBorrowAsync(user.Id);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Account is deleted.");
        }

        [Fact]
        public async Task EnsureUserCanBorrowAsync_ShouldThrow_WhenUserIsBanned()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser();
            user.IsBanned = true;

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            Func<Task> act = async () =>
                await service.EnsureUserCanBorrowAsync(user.Id);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Account is banned.");
        }

        [Fact]
        public async Task EnsureUserCanBorrowAsync_ShouldThrow_WhenScoreIsTooLow()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser(score: 10);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            Func<Task> act = async () =>
                await service.EnsureUserCanBorrowAsync(user.Id);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Your score is too low to borrow items.");
        }

        [Fact]
        public async Task EnsureUserCanBorrowAsync_ShouldNotThrow_WhenUserCanBorrow()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser(score: 100);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            Func<Task> act = async () =>
                await service.EnsureUserCanBorrowAsync(user.Id);

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task EnsureUserIsActiveAsync_ShouldThrow_WhenUserIsBanned()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser();
            user.IsBanned = true;

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            Func<Task> act = async () =>
                await service.EnsureUserIsActiveAsync(user.Id);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Account is banned.");
        }

        [Fact]
        public async Task GetTotalUsersCountAsync_ShouldReturnRepositoryCount()
        {
            // Arrange
            var service = CreateService();

            _userRepositoryMock
                .Setup(r => r.GetTotalUsersCountAsync())
                .ReturnsAsync(42);

            // Act
            var result = await service.GetTotalUsersCountAsync();

            // Assert
            result.Should().Be(42);
        }
    }
}