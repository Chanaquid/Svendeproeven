using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace backend.Tests.Services
{
    public class UserBanHistoryServiceTests
    {
        private readonly Mock<IUserBanHistoryRepository> _banHistoryRepositoryMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();

        private UserBanHistoryService CreateService()
        {
            return new UserBanHistoryService(
                _banHistoryRepositoryMock.Object,
                _userRepositoryMock.Object
            );
        }

        private static ApplicationUser CreateUser(string id = "user-123")
        {
            return new ApplicationUser
            {
                Id = id,
                FullName = "Test User",
                UserName = "testuser",
                AvatarUrl = "avatar.png"
            };
        }

        private static ApplicationUser CreateAdmin(string id = "admin-123")
        {
            return new ApplicationUser
            {
                Id = id,
                FullName = "Admin User",
                UserName = "adminuser",
                AvatarUrl = "admin.png"
            };
        }

        private static UserBanHistory CreateBanHistory()
        {
            var user = CreateUser();
            var admin = CreateAdmin();

            return new UserBanHistory
            {
                Id = 1,
                UserId = user.Id,
                User = user,
                AdminId = admin.Id,
                Admin = admin,
                IsBanned = true,
                Reason = "Rule violation",
                Note = "Admin note",
                BannedAt = DateTime.UtcNow,
                BanExpiresAt = DateTime.UtcNow.AddDays(7)
            };
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldThrow_WhenUserDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync("missing-user"))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            Func<Task> act = async () =>
                await service.GetByUserIdAsync("missing-user", null, request);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("User not found.");

            _banHistoryRepositoryMock.Verify(
                r => r.GetByUserIdAsync(It.IsAny<string>(), It.IsAny<UserBanHistoryFilter?>(), It.IsAny<PagedRequest>()),
                Times.Never);
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldReturnPagedBanHistory_WhenUserExists()
        {
            // Arrange
            var service = CreateService();

            var entry = CreateBanHistory();
            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(entry.UserId))
                .ReturnsAsync(entry.User);

            _banHistoryRepositoryMock
                .Setup(r => r.GetByUserIdAsync(entry.UserId, null, request))
                .ReturnsAsync(new PagedResult<UserBanHistory>
                {
                    Items = new List<UserBanHistory> { entry },
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 10
                });

            // Act
            var result = await service.GetByUserIdAsync(entry.UserId, null, request);

            // Assert
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);

            var dto = result.Items[0];
            dto.Id.Should().Be(entry.Id);
            dto.BannedUserId.Should().Be(entry.UserId);
            dto.BannedFullName.Should().Be(entry.User!.FullName);
            dto.BannedUserName.Should().Be(entry.User.UserName);
            dto.AdminId.Should().Be(entry.AdminId);
            dto.AdminFullName.Should().Be(entry.Admin!.FullName);
            dto.IsBanned.Should().BeTrue();
            dto.Reason.Should().Be("Rule violation");
            dto.Note.Should().Be("Admin note");
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPagedBanHistory()
        {
            // Arrange
            var service = CreateService();

            var entry = CreateBanHistory();
            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            _banHistoryRepositoryMock
                .Setup(r => r.GetAllAsync(null, request))
                .ReturnsAsync(new PagedResult<UserBanHistory>
                {
                    Items = new List<UserBanHistory> { entry },
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 10
                });

            // Act
            var result = await service.GetAllAsync(null, request);

            // Assert
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);

            result.Items[0].Id.Should().Be(entry.Id);
            result.Items[0].BannedUserId.Should().Be(entry.UserId);
            result.Items[0].AdminId.Should().Be(entry.AdminId);
            result.Items[0].Reason.Should().Be(entry.Reason);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrow_WhenEntryDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            _banHistoryRepositoryMock
                .Setup(r => r.GetByIdAsync(99))
                .ReturnsAsync((UserBanHistory?)null);

            // Act
            Func<Task> act = async () => await service.GetByIdAsync(99);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Ban history entry 99 not found.");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnBanHistory_WhenEntryExists()
        {
            // Arrange
            var service = CreateService();

            var entry = CreateBanHistory();

            _banHistoryRepositoryMock
                .Setup(r => r.GetByIdAsync(entry.Id))
                .ReturnsAsync(entry);

            // Act
            var result = await service.GetByIdAsync(entry.Id);

            // Assert
            result.Id.Should().Be(entry.Id);
            result.BannedUserId.Should().Be(entry.UserId);
            result.BannedFullName.Should().Be(entry.User!.FullName);
            result.BannedUserName.Should().Be(entry.User.UserName);
            result.BannedUserAvatarUrl.Should().Be(entry.User.AvatarUrl);

            result.AdminId.Should().Be(entry.AdminId);
            result.AdminFullName.Should().Be(entry.Admin!.FullName);
            result.AdminUserName.Should().Be(entry.Admin.UserName);
            result.AdminAvatarUrl.Should().Be(entry.Admin.AvatarUrl);

            result.IsBanned.Should().Be(entry.IsBanned);
            result.Reason.Should().Be(entry.Reason);
            result.Note.Should().Be(entry.Note);
            result.BannedAt.Should().Be(entry.BannedAt);
            result.BanExpiresAt.Should().Be(entry.BanExpiresAt);
        }
    }
}