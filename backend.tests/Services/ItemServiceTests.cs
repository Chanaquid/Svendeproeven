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
    public class ItemServiceTests
    {
        private readonly Mock<IItemRepository> _itemRepositoryMock = new();
        private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IItemReviewRepository> _itemReviewRepositoryMock = new();
        private readonly Mock<ILoanRepository> _loanRepositoryMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly Mock<IUserFavoriteRepository> _userFavoriteRepositoryMock = new();
        private readonly Mock<IUserBlockRepository> _userBlockRepositoryMock = new();
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

        public ItemServiceTests()
        {
            _userManagerMock = MockUserManager();
        }

        private ItemService CreateService()
        {
            return new ItemService(
                _itemRepositoryMock.Object,
                _categoryRepositoryMock.Object,
                _userRepositoryMock.Object,
                _itemReviewRepositoryMock.Object,
                _loanRepositoryMock.Object,
                _notificationServiceMock.Object,
                _userManagerMock.Object,
                _userFavoriteRepositoryMock.Object,
                _userBlockRepositoryMock.Object
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
                AvatarUrl = "avatar.png",
                Score = 100,
                IsVerified = true
            };
        }

        private static Category CreateCategory()
        {
            return new Category
            {
                Id = 1,
                Name = "Tools",
                Slug = "tools",
                Icon = "tool-icon",
                IsActive = true
            };
        }

        private static Item CreateItem(string ownerId = "user-123")
        {
            var owner = CreateUser(ownerId);
            var category = CreateCategory();

            return new Item
            {
                Id = 1,
                OwnerId = ownerId,
                Owner = owner,
                CategoryId = category.Id,
                Category = category,
                Title = "Hammer",
                Slug = "hammer",
                Description = "Good hammer",
                CurrentValue = 200,
                PricePerDay = 20,
                IsFree = false,
                PickupAddress = "Test Address",
                PickupLatitude = 55.1,
                PickupLongitude = 12.1,
                AvailableFrom = DateTime.UtcNow.AddDays(1),
                AvailableUntil = DateTime.UtcNow.AddDays(10),
                Status = ItemStatus.Approved,
                Availability = ItemAvailability.Available,
                IsActive = true,
                QrCode = "ABC123456789",
                CreatedAt = DateTime.UtcNow,
                Photos = new List<ItemPhoto>(),
                Reviews = new List<ItemReview>(),
                Loans = new List<Loan>()
            };
        }

        private static CreateItemDto CreateValidCreateItemDto()
        {
            return new CreateItemDto
            {
                CategoryId = 1,
                Title = "Hammer",
                Description = "Good hammer",
                CurrentValue = 200,
                PricePerDay = 20,
                IsFree = false,
                PickupAddress = "Test Address",
                PickupLatitude = 55.1,
                PickupLongitude = 12.1,
                AvailableFrom = DateTime.UtcNow.AddDays(1),
                AvailableUntil = DateTime.UtcNow.AddDays(10),
                MinLoanDays = 1,
                MaxLoanDays = 5,
                RequiresVerification = false
            };
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnItem_WhenItemExists()
        {
            // Arrange
            var service = CreateService();

            var item = CreateItem();

            _itemRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(item.Id))
                .ReturnsAsync(item);

            _userFavoriteRepositoryMock
                .Setup(r => r.ExistsAsync("user-123", item.Id))
                .ReturnsAsync(true);

            // Act
            var result = await service.GetByIdAsync(item.Id, "user-123");

            // Assert
            result.Id.Should().Be(item.Id);
            result.Title.Should().Be(item.Title);
            result.OwnerId.Should().Be(item.OwnerId);
            result.IsMine.Should().BeTrue();
            result.IsFavoritedByCurrentUser.Should().BeTrue();
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrow_WhenItemDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            _itemRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(99))
                .ReturnsAsync((Item?)null);

            // Act
            Func<Task> act = async () => await service.GetByIdAsync(99, "user-123");

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Item 99 not found");
        }

        [Fact]
        public async Task CreateItemAsync_ShouldThrow_WhenOwnerDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var dto = CreateValidCreateItemDto();

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync("missing-user"))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            Func<Task> act = async () => await service.CreateItemAsync("missing-user", dto);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Owner not found.");

            _itemRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Item>()), Times.Never);
        }

        [Fact]
        public async Task CreateItemAsync_ShouldThrow_WhenCategoryDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var owner = CreateUser();
            var dto = CreateValidCreateItemDto();

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(owner.Id))
                .ReturnsAsync(owner);

            _categoryRepositoryMock
                .Setup(r => r.GetByIdAsync(dto.CategoryId))
                .ReturnsAsync((Category?)null);

            // Act
            Func<Task> act = async () => await service.CreateItemAsync(owner.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage($"Category with ID {dto.CategoryId} does not exist.");

            _itemRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Item>()), Times.Never);
        }

        [Fact]
        public async Task CreateItemAsync_ShouldThrow_WhenCurrentValueIsNegative()
        {
            // Arrange
            var service = CreateService();

            var owner = CreateUser();
            var category = CreateCategory();

            var dto = CreateValidCreateItemDto();
            dto.CurrentValue = -1;

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(owner.Id))
                .ReturnsAsync(owner);

            _categoryRepositoryMock
                .Setup(r => r.GetByIdAsync(dto.CategoryId))
                .ReturnsAsync(category);

            // Act
            Func<Task> act = async () => await service.CreateItemAsync(owner.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("Current value cant be negative");

            _itemRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Item>()), Times.Never);
        }

        [Fact]
        public async Task CreateItemAsync_ShouldCreateItem_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var owner = CreateUser();
            var category = CreateCategory();
            var dto = CreateValidCreateItemDto();

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(owner.Id))
                .ReturnsAsync(owner);

            _categoryRepositoryMock
                .Setup(r => r.GetByIdAsync(dto.CategoryId))
                .ReturnsAsync(category);

            _itemRepositoryMock
                .Setup(r => r.GetBySlugAsync("hammer"))
                .ReturnsAsync((Item?)null);

            _itemRepositoryMock
                .Setup(r => r.QrCodeExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            _itemRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Item>()))
                .Callback<Item>(item =>
                {
                    item.Id = 1;
                    item.Owner = owner;
                    item.Category = category;
                    item.Photos = new List<ItemPhoto>();
                    item.Reviews = new List<ItemReview>();
                    item.Loans = new List<Loan>();
                })
                .Returns(Task.CompletedTask);

            _itemRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _notificationServiceMock
                .Setup(n => n.SendToAdminsAsync(
                    It.IsAny<NotificationType>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<NotificationReferenceType?>()))
                .Returns(Task.CompletedTask);

            _itemRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(1))
                .ReturnsAsync(CreateItem(owner.Id));

            _userFavoriteRepositoryMock
                .Setup(r => r.ExistsAsync(owner.Id, 1))
                .ReturnsAsync(false);

            // Act
            var result = await service.CreateItemAsync(owner.Id, dto);

            // Assert
            result.Id.Should().Be(1);
            result.Title.Should().Be("Hammer");
            result.OwnerId.Should().Be(owner.Id);

            _itemRepositoryMock.Verify(r => r.AddAsync(It.Is<Item>(item =>
                item.OwnerId == owner.Id &&
                item.CategoryId == dto.CategoryId &&
                item.Title == "Hammer" &&
                item.Description == "Good hammer" &&
                item.Status == ItemStatus.Pending &&
                item.Availability == ItemAvailability.Available &&
                item.IsActive
            )), Times.Once);

            _itemRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateItemAsync_ShouldThrow_WhenItemDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var dto = new UpdateItemDto
            {
                Title = "Updated title"
            };

            _itemRepositoryMock
                .Setup(r => r.GetByIdAsync(99))
                .ReturnsAsync((Item?)null);

            // Act
            Func<Task> act = async () => await service.UpdateItemAsync("user-123", 99, dto);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Item 99 not found.");
        }

        [Fact]
        public async Task UpdateItemAsync_ShouldThrow_WhenUserIsNotOwnerOrAdmin()
        {
            // Arrange
            var service = CreateService();

            var item = CreateItem(ownerId: "owner-123");
            var user = CreateUser("another-user");

            var dto = new UpdateItemDto
            {
                Title = "Updated title"
            };

            _itemRepositoryMock
                .Setup(r => r.GetByIdAsync(item.Id))
                .ReturnsAsync(item);

            _userManagerMock
                .Setup(m => m.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(m => m.IsInRoleAsync(user, "Admin"))
                .ReturnsAsync(false);

            // Act
            Func<Task> act = async () => await service.UpdateItemAsync(user.Id, item.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You do not have permission to edit this item");
        }

        [Fact]
        public async Task ToggleActiveStatusAsync_ShouldChangeActiveStatus_WhenOwner()
        {
            // Arrange
            var service = CreateService();

            var item = CreateItem();
            var owner = item.Owner!;

            _itemRepositoryMock
                .Setup(r => r.GetByIdAsync(item.Id))
                .ReturnsAsync(item);

            _userManagerMock
                .Setup(m => m.FindByIdAsync(owner.Id))
                .ReturnsAsync(owner);

            _userManagerMock
                .Setup(m => m.IsInRoleAsync(owner, "Admin"))
                .ReturnsAsync(false);

            _itemRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _itemRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(item.Id))
                .ReturnsAsync(item);

            _userFavoriteRepositoryMock
                .Setup(r => r.ExistsAsync(owner.Id, item.Id))
                .ReturnsAsync(false);

            // Act
            var result = await service.ToggleActiveStatusAsync(owner.Id, item.Id, false);

            // Assert
            item.IsActive.Should().BeFalse();
            result.IsActive.Should().BeFalse();

            _itemRepositoryMock.Verify(r => r.Update(item), Times.Once);
            _itemRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetQrCodeAsync_ShouldReturnQrCode_WhenOwner()
        {
            // Arrange
            var service = CreateService();

            var item = CreateItem();
            var owner = item.Owner!;

            _itemRepositoryMock
                .Setup(r => r.GetByIdAsync(item.Id))
                .ReturnsAsync(item);

            _userManagerMock
                .Setup(m => m.FindByIdAsync(owner.Id))
                .ReturnsAsync(owner);

            _userManagerMock
                .Setup(m => m.IsInRoleAsync(owner, "Admin"))
                .ReturnsAsync(false);

            // Act
            var result = await service.GetQrCodeAsync(owner.Id, item.Id);

            // Assert
            result.ItemId.Should().Be(item.Id);
            result.QrCode.Should().Be(item.QrCode);
        }

        [Fact]
        public async Task GetQrCodeAsync_ShouldThrow_WhenUserIsNotOwnerOrAdmin()
        {
            // Arrange
            var service = CreateService();

            var item = CreateItem(ownerId: "owner-123");
            var user = CreateUser("another-user");

            _itemRepositoryMock
                .Setup(r => r.GetByIdAsync(item.Id))
                .ReturnsAsync(item);

            _userManagerMock
                .Setup(m => m.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(m => m.IsInRoleAsync(user, "Admin"))
                .ReturnsAsync(false);

            // Act
            Func<Task> act = async () => await service.GetQrCodeAsync(user.Id, item.Id);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Only the owner or an admin can view the QR code");
        }

        [Fact]
        public async Task DecideItemAsync_ShouldApprovePendingItem_WhenApproved()
        {
            // Arrange
            var service = CreateService();

            var item = CreateItem();
            item.Status = ItemStatus.Pending;

            var dto = new AdminDecideItemDto
            {
                IsApproved = true,
                AdminNote = "Looks good"
            };

            _itemRepositoryMock
                .Setup(r => r.GetByIdAsync(item.Id))
                .ReturnsAsync(item);

            _itemRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _notificationServiceMock
                .Setup(n => n.SendAsync(
                    It.IsAny<string>(),
                    It.IsAny<NotificationType>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<NotificationReferenceType?>()))
                .Returns(Task.CompletedTask);

            _itemRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(item.Id))
                .ReturnsAsync(item);

            // Act
            var result = await service.DecideItemAsync("admin-123", item.Id, dto);

            // Assert
            item.Status.Should().Be(ItemStatus.Approved);
            item.AdminNote.Should().Be(dto.AdminNote);
            item.ReviewedByAdminId.Should().Be("admin-123");
            item.ReviewedAt.Should().NotBeNull();

            result.Status.Should().Be(ItemStatus.Approved);

            _itemRepositoryMock.Verify(r => r.Update(item), Times.Once);
            _itemRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DecideItemAsync_ShouldThrow_WhenItemIsNotPending()
        {
            // Arrange
            var service = CreateService();

            var item = CreateItem();
            item.Status = ItemStatus.Approved;

            var dto = new AdminDecideItemDto
            {
                IsApproved = true,
                AdminNote = "Looks good"
            };

            _itemRepositoryMock
                .Setup(r => r.GetByIdAsync(item.Id))
                .ReturnsAsync(item);

            // Act
            Func<Task> act = async () => await service.DecideItemAsync("admin-123", item.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Only pending items can be approved or rejected");
        }

        [Fact]
        public async Task GetPendingApprovalsCountAsync_ShouldReturnRepositoryCount()
        {
            // Arrange
            var service = CreateService();

            _itemRepositoryMock
                .Setup(r => r.GetPendingApprovalsCountAsync())
                .ReturnsAsync(7);

            // Act
            var result = await service.GetPendingApprovalsCountAsync();

            // Assert
            result.Should().Be(7);
        }

        [Fact]
        public async Task SlugExistsAsync_ShouldReturnTrue_WhenSlugExists()
        {
            // Arrange
            var service = CreateService();

            var item = CreateItem();

            _itemRepositoryMock
                .Setup(r => r.GetBySlugAsync("hammer"))
                .ReturnsAsync(item);

            // Act
            var result = await service.SlugExistsAsync("hammer");

            // Assert
            result.Should().BeTrue();
        }
    }
}