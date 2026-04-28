using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace backend.Tests.Services
{
    public class UserFavoriteServiceTests
    {
        private readonly Mock<IUserFavoriteRepository> _userFavoriteRepositoryMock = new();
        private readonly Mock<IItemRepository> _itemRepositoryMock = new();

        private UserFavoriteService CreateService()
        {
            return new UserFavoriteService(
                _userFavoriteRepositoryMock.Object,
                _itemRepositoryMock.Object
            );
        }

        private static ApplicationUser CreateOwner()
        {
            return new ApplicationUser
            {
                Id = "owner-123",
                FullName = "Owner User",
                UserName = "owneruser",
                AvatarUrl = "owner.png",
                Score = 90,
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
                Icon = "tool-icon"
            };
        }

        private static Item CreateItem()
        {
            var owner = CreateOwner();
            var category = CreateCategory();

            return new Item
            {
                Id = 1,
                OwnerId = owner.Id,
                Owner = owner,
                CategoryId = category.Id,
                Category = category,
                Title = "Hammer",
                Description = "Good hammer",
                Slug = "hammer",
                PickupAddress = "Test Address",
                IsFree = false,
                PricePerDay = 25,
                Condition = ItemCondition.Good,
                Availability = ItemAvailability.Available,
                Status = ItemStatus.Approved,
                IsActive = true,
                MinLoanDays = 1,
                MaxLoanDays = 5,
                AvailableFrom = DateTime.UtcNow.AddDays(1),
                AvailableUntil = DateTime.UtcNow.AddDays(10),
                CreatedAt = DateTime.UtcNow,
                Photos = new List<ItemPhoto>
                {
                    new ItemPhoto
                    {
                        Id = 1,
                        PhotoUrl = "photo.jpg",
                        IsPrimary = true
                    }
                },
                Reviews = new List<ItemReview>
                {
                    new ItemReview
                    {
                        Id = 1,
                        Rating = 4
                    },
                    new ItemReview
                    {
                        Id = 2,
                        Rating = 5
                    }
                }
            };
        }

        private static UserFavoriteItem CreateFavorite()
        {
            var item = CreateItem();

            return new UserFavoriteItem
            {
                UserId = "user-123",
                ItemId = item.Id,
                Item = item,
                NotifyWhenAvailable = true,
                SavedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task GetFavoritesAsync_ShouldReturnPagedFavoriteItems()
        {
            // Arrange
            var service = CreateService();

            var favorite = CreateFavorite();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            _userFavoriteRepositoryMock
                .Setup(r => r.GetAllByUserIdAsync("user-123", request))
                .ReturnsAsync(new PagedResult<UserFavoriteItem>
                {
                    Items = new List<UserFavoriteItem> { favorite },
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 10
                });

            // Act
            var result = await service.GetFavoritesAsync("user-123", request);

            // Assert
            result.Items.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);

            var dto = result.Items[0];
            dto.Id.Should().Be(favorite.Item.Id);
            dto.Title.Should().Be(favorite.Item.Title);
            dto.Description.Should().Be(favorite.Item.Description);
            dto.Slug.Should().Be(favorite.Item.Slug);
            dto.MainPhotoUrl.Should().Be("photo.jpg");
            dto.OwnerId.Should().Be(favorite.Item.OwnerId);
            dto.OwnerName.Should().Be(favorite.Item.Owner!.FullName);
            dto.CategoryName.Should().Be(favorite.Item.Category!.Name);
            dto.NotifyWhenAvailable.Should().BeTrue();
            dto.TotalReviews.Should().Be(2);
            dto.AverageRating.Should().Be(4.5);
        }

        [Fact]
        public async Task ToggleFavoriteAsync_ShouldThrow_WhenItemDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            _itemRepositoryMock
                .Setup(r => r.GetByIdAsync(99))
                .ReturnsAsync((Item?)null);

            // Act
            Func<Task> act = async () =>
                await service.ToggleFavoriteAsync("user-123", 99, notify: false);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Item 99 not found");

            _userFavoriteRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserFavoriteItem>()), Times.Never);
        }

        [Fact]
        public async Task ToggleFavoriteAsync_ShouldThrow_WhenItemIsNotApproved()
        {
            // Arrange
            var service = CreateService();

            var item = CreateItem();
            item.Status = ItemStatus.Pending;

            _itemRepositoryMock
                .Setup(r => r.GetByIdAsync(item.Id))
                .ReturnsAsync(item);

            // Act
            Func<Task> act = async () =>
                await service.ToggleFavoriteAsync("user-123", item.Id, notify: false);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("You can only favorite active, approved items.");

            _userFavoriteRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserFavoriteItem>()), Times.Never);
        }

        [Fact]
        public async Task ToggleFavoriteAsync_ShouldThrow_WhenItemIsInactive()
        {
            // Arrange
            var service = CreateService();

            var item = CreateItem();
            item.IsActive = false;

            _itemRepositoryMock
                .Setup(r => r.GetByIdAsync(item.Id))
                .ReturnsAsync(item);

            // Act
            Func<Task> act = async () =>
                await service.ToggleFavoriteAsync("user-123", item.Id, notify: false);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("You can only favorite active, approved items.");

            _userFavoriteRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserFavoriteItem>()), Times.Never);
        }

        [Fact]
        public async Task ToggleFavoriteAsync_ShouldThrow_WhenUserFavoritesOwnItem()
        {
            // Arrange
            var service = CreateService();

            var item = CreateItem();

            _itemRepositoryMock
                .Setup(r => r.GetByIdAsync(item.Id))
                .ReturnsAsync(item);

            // Act
            Func<Task> act = async () =>
                await service.ToggleFavoriteAsync(item.OwnerId, item.Id, notify: false);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("You cannot favorite your own item.");

            _userFavoriteRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserFavoriteItem>()), Times.Never);
        }

        [Fact]
        public async Task ToggleFavoriteAsync_ShouldRemoveFavorite_WhenAlreadyFavorited()
        {
            // Arrange
            var service = CreateService();

            var item = CreateItem();

            var favorite = new UserFavoriteItem
            {
                UserId = "user-123",
                ItemId = item.Id,
                Item = item,
                NotifyWhenAvailable = true,
                SavedAt = DateTime.UtcNow
            };

            _itemRepositoryMock
                .Setup(r => r.GetByIdAsync(item.Id))
                .ReturnsAsync(item);

            _userFavoriteRepositoryMock
                .Setup(r => r.GetAsync("user-123", item.Id))
                .ReturnsAsync(favorite);

            _userFavoriteRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await service.ToggleFavoriteAsync("user-123", item.Id, notify: false);

            // Assert
            result.Should().BeFalse();

            _userFavoriteRepositoryMock.Verify(r => r.Remove(favorite), Times.Once);
            _userFavoriteRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
            _userFavoriteRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserFavoriteItem>()), Times.Never);
        }

        [Fact]
        public async Task ToggleFavoriteAsync_ShouldAddFavorite_WhenNotAlreadyFavorited()
        {
            // Arrange
            var service = CreateService();

            var item = CreateItem();

            _itemRepositoryMock
                .Setup(r => r.GetByIdAsync(item.Id))
                .ReturnsAsync(item);

            _userFavoriteRepositoryMock
                .Setup(r => r.GetAsync("user-123", item.Id))
                .ReturnsAsync((UserFavoriteItem?)null);

            _userFavoriteRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<UserFavoriteItem>()))
                .Returns(Task.CompletedTask);

            _userFavoriteRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await service.ToggleFavoriteAsync("user-123", item.Id, notify: true);

            // Assert
            result.Should().BeTrue();

            _userFavoriteRepositoryMock.Verify(r => r.AddAsync(It.Is<UserFavoriteItem>(f =>
                f.UserId == "user-123" &&
                f.ItemId == item.Id &&
                f.NotifyWhenAvailable == true
            )), Times.Once);

            _userFavoriteRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task IsFavoritedAsync_ShouldReturnRepositoryResult()
        {
            // Arrange
            var service = CreateService();

            _userFavoriteRepositoryMock
                .Setup(r => r.ExistsAsync("user-123", 1))
                .ReturnsAsync(true);

            // Act
            var result = await service.IsFavoritedAsync("user-123", 1);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateNotifyPreferenceAsync_ShouldThrow_WhenFavoriteDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            _userFavoriteRepositoryMock
                .Setup(r => r.GetAsync("user-123", 1))
                .ReturnsAsync((UserFavoriteItem?)null);

            // Act
            Func<Task> act = async () =>
                await service.UpdateNotifyPreferenceAsync("user-123", 1, notify: true);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Favorite not found.");
        }

        [Fact]
        public async Task UpdateNotifyPreferenceAsync_ShouldUpdateFavorite_WhenExists()
        {
            // Arrange
            var service = CreateService();

            var favorite = CreateFavorite();
            favorite.NotifyWhenAvailable = false;

            _userFavoriteRepositoryMock
                .Setup(r => r.GetAsync(favorite.UserId, favorite.ItemId))
                .ReturnsAsync(favorite);

            _userFavoriteRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.UpdateNotifyPreferenceAsync(favorite.UserId, favorite.ItemId, notify: true);

            // Assert
            favorite.NotifyWhenAvailable.Should().BeTrue();

            _userFavoriteRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}