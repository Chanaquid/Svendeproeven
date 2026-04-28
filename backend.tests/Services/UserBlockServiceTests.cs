using backend.Common;
using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace backend.Tests.Services
{
    public class UserBlockServiceTests
    {
        private readonly Mock<IUserBlockRepository> _blockRepositoryMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

        public UserBlockServiceTests()
        {
            _userManagerMock = MockUserManager();
        }

        private UserBlockService CreateService()
        {
            return new UserBlockService(
                _blockRepositoryMock.Object,
                _userRepositoryMock.Object,
                _userManagerMock.Object
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

        private static UserBlock CreateBlock()
        {
            var blocker = CreateUser("blocker-123");
            blocker.FullName = "Blocker User";
            blocker.UserName = "blocker";

            var blocked = CreateUser("blocked-123");
            blocked.FullName = "Blocked User";
            blocked.UserName = "blocked";

            return new UserBlock
            {
                BlockerId = blocker.Id,
                Blocker = blocker,
                BlockedId = blocked.Id,
                Blocked = blocked,
                CreatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task BlockUserAsync_ShouldThrow_WhenUserBlocksThemself()
        {
            // Arrange
            var service = CreateService();

            // Act
            Func<Task> act = async () =>
                await service.BlockUserAsync("user-123", "user-123");

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("You cannot block yourself.");

            _blockRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserBlock>()), Times.Never);
        }

        [Fact]
        public async Task BlockUserAsync_ShouldThrow_WhenBlockerDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync("blocker-123"))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            Func<Task> act = async () =>
                await service.BlockUserAsync("blocker-123", "blocked-123");

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Blocker not found.");

            _blockRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserBlock>()), Times.Never);
        }

        [Fact]
        public async Task BlockUserAsync_ShouldThrow_WhenBlockedUserDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var blocker = CreateUser("blocker-123");

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(blocker.Id))
                .ReturnsAsync(blocker);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync("missing-user"))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            Func<Task> act = async () =>
                await service.BlockUserAsync(blocker.Id, "missing-user");

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("User to block not found.");

            _blockRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserBlock>()), Times.Never);
        }

        [Fact]
        public async Task BlockUserAsync_ShouldThrow_WhenBlockedUserIsAdmin()
        {
            // Arrange
            var service = CreateService();

            var blocker = CreateUser("blocker-123");
            var blockedAdmin = CreateUser("admin-123");

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(blocker.Id))
                .ReturnsAsync(blocker);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(blockedAdmin.Id))
                .ReturnsAsync(blockedAdmin);

            _userManagerMock
                .Setup(m => m.GetRolesAsync(blockedAdmin))
                .ReturnsAsync(new List<string> { Roles.Admin });

            // Act
            Func<Task> act = async () =>
                await service.BlockUserAsync(blocker.Id, blockedAdmin.Id);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Admins cannot be blocked.");

            _blockRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserBlock>()), Times.Never);
        }


        [Fact]
        public async Task BlockUserAsync_ShouldThrow_WhenAlreadyBlocked()
        {
            // Arrange
            var service = CreateService();

            var blocker = CreateUser("blocker-123");
            var blocked = CreateUser("blocked-123");

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(blocker.Id))
                .ReturnsAsync(blocker);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(blocked.Id))
                .ReturnsAsync(blocked);

            _userManagerMock
                .Setup(m => m.GetRolesAsync(blocked))
                .ReturnsAsync(new List<string> { Roles.User });

            _userManagerMock
                .Setup(m => m.GetRolesAsync(blocker))
                .ReturnsAsync(new List<string> { Roles.User });

            _blockRepositoryMock
                .Setup(r => r.IsBlockedAsync(blocker.Id, blocked.Id))
                .ReturnsAsync(true);

            // Act
            Func<Task> act = async () =>
                await service.BlockUserAsync(blocker.Id, blocked.Id);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("You have already blocked this user.");

            _blockRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserBlock>()), Times.Never);
        }

        [Fact]
        public async Task BlockUserAsync_ShouldAddBlock_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var blocker = CreateUser("blocker-123");
            var blocked = CreateUser("blocked-123");

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(blocker.Id))
                .ReturnsAsync(blocker);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(blocked.Id))
                .ReturnsAsync(blocked);

            _userManagerMock
                .Setup(m => m.GetRolesAsync(blocked))
                .ReturnsAsync(new List<string> { Roles.User });

            _userManagerMock
                .Setup(m => m.GetRolesAsync(blocker))
                .ReturnsAsync(new List<string> { Roles.User });

            _blockRepositoryMock
                .Setup(r => r.IsBlockedAsync(blocker.Id, blocked.Id))
                .ReturnsAsync(false);

            _blockRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<UserBlock>()))
                .Returns(Task.CompletedTask);

            _blockRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.BlockUserAsync(blocker.Id, blocked.Id);

            // Assert
            _blockRepositoryMock.Verify(r => r.AddAsync(It.Is<UserBlock>(b =>
                b.BlockerId == blocker.Id &&
                b.BlockedId == blocked.Id
            )), Times.Once);

            _blockRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UnblockUserAsync_ShouldThrow_WhenBlockDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            _blockRepositoryMock
                .Setup(r => r.GetAsync("blocker-123", "blocked-123"))
                .ReturnsAsync((UserBlock?)null);

            // Act
            Func<Task> act = async () =>
                await service.UnblockUserAsync("blocker-123", "blocked-123");

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Block relationship not found.");
        }

        [Fact]
        public async Task UnblockUserAsync_ShouldDeleteBlock_WhenExists()
        {
            // Arrange
            var service = CreateService();

            var block = CreateBlock();

            _blockRepositoryMock
                .Setup(r => r.GetAsync(block.BlockerId, block.BlockedId))
                .ReturnsAsync(block);

            _blockRepositoryMock
                .Setup(r => r.DeleteAsync(block))
                .Returns(Task.CompletedTask);

            _blockRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.UnblockUserAsync(block.BlockerId, block.BlockedId);

            // Assert
            _blockRepositoryMock.Verify(r => r.DeleteAsync(block), Times.Once);
            _blockRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task IsBlockedAsync_ShouldReturnRepositoryResult()
        {
            // Arrange
            var service = CreateService();

            _blockRepositoryMock
                .Setup(r => r.IsBlockedAsync("user-1", "user-2"))
                .ReturnsAsync(true);

            // Act
            var result = await service.IsBlockedAsync("user-1", "user-2");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task AreBlockedEitherWayAsync_ShouldReturnRepositoryResult()
        {
            // Arrange
            var service = CreateService();

            _blockRepositoryMock
                .Setup(r => r.AreBlockedEitherWayAsync("user-1", "user-2"))
                .ReturnsAsync(true);

            // Act
            var result = await service.AreBlockedEitherWayAsync("user-1", "user-2");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task GetMyBlocksAsync_ShouldReturnPagedBlocks()
        {
            // Arrange
            var service = CreateService();

            var block = CreateBlock();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            _blockRepositoryMock
                .Setup(r => r.GetBlocksByBlockerAsync(block.BlockerId, null, request))
                .ReturnsAsync(new PagedResult<UserBlock>
                {
                    Items = new List<UserBlock> { block },
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 10
                });

            // Act
            var result = await service.GetMyBlocksAsync(block.BlockerId, null, request);

            // Assert
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);

            var dto = result.Items[0];
            dto.BlockedId.Should().Be(block.BlockedId);
            dto.BlockedName.Should().Be(block.Blocked!.FullName);
            dto.BlockedUserName.Should().Be(block.Blocked.UserName);
            dto.BlockedAvatarUrl.Should().Be(block.Blocked.AvatarUrl);
            dto.CreatedAt.Should().Be(block.CreatedAt);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPagedBlocks()
        {
            // Arrange
            var service = CreateService();

            var block = CreateBlock();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            _blockRepositoryMock
                .Setup(r => r.GetAllAsync(null, request))
                .ReturnsAsync(new PagedResult<UserBlock>
                {
                    Items = new List<UserBlock> { block },
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 10
                });

            // Act
            var result = await service.GetAllAsync(null, request);

            // Assert
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);

            var dto = result.Items[0];
            dto.BlockerId.Should().Be(block.BlockerId);
            dto.BlockerName.Should().Be(block.Blocker!.FullName);
            dto.BlockerUserName.Should().Be(block.Blocker.UserName);
            dto.BlockedId.Should().Be(block.BlockedId);
            dto.BlockedName.Should().Be(block.Blocked!.FullName);
            dto.BlockedUserName.Should().Be(block.Blocked.UserName);
        }

        [Fact]
        public async Task AdminUnblockAsync_ShouldThrow_WhenBlockDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            _blockRepositoryMock
                .Setup(r => r.GetAsync("blocker-123", "blocked-123"))
                .ReturnsAsync((UserBlock?)null);

            // Act
            Func<Task> act = async () =>
                await service.AdminUnblockAsync("blocker-123", "blocked-123");

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Block relationship not found.");
        }

        [Fact]
        public async Task AdminUnblockAsync_ShouldDeleteBlock_WhenExists()
        {
            // Arrange
            var service = CreateService();

            var block = CreateBlock();

            _blockRepositoryMock
                .Setup(r => r.GetAsync(block.BlockerId, block.BlockedId))
                .ReturnsAsync(block);

            _blockRepositoryMock
                .Setup(r => r.DeleteAsync(block))
                .Returns(Task.CompletedTask);

            _blockRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.AdminUnblockAsync(block.BlockerId, block.BlockedId);

            // Assert
            _blockRepositoryMock.Verify(r => r.DeleteAsync(block), Times.Once);
            _blockRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}