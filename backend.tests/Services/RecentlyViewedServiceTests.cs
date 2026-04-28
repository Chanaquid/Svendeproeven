using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace backend.Tests.Services
{
    public class UserRecentlyViewedServiceTests
    {
        private readonly Mock<IUserRecentlyViewedRepository> _recentlyViewedRepositoryMock = new();
        private readonly Mock<IItemRepository> _itemRepositoryMock = new();

        private UserRecentlyViewedService CreateService()
        {
            return new UserRecentlyViewedService(
                _recentlyViewedRepositoryMock.Object,
                _itemRepositoryMock.Object
            );
        }

        private static Item CreateItem()
        {
            return new Item
            {
                Id = 1,
                Title = "Hammer",
                Slug = "hammer",
                PricePerDay = 25,
                IsFree = false,
                IsActive = true,
                IsDeleted = false,
                Availability = ItemAvailability.Available,
                Owner = new ApplicationUser
                {
                    Id = "owner-123",
                    FullName = "Owner User"
                },
                Photos = new List<ItemPhoto>
                {
                    new ItemPhoto
                    {
                        Id = 1,
                        PhotoUrl = "https://example.com/photo.jpg",
                        DisplayOrder = 1
                    }
                }
            };
        }

        [Fact]
        public async Task TrackViewAsync_ShouldUpdateExistingView_WhenItemAlreadyExists()
        {
            // Arrange
            var service = CreateService();

            var existing = new UserRecentlyViewedItem
            {
                UserId = "user-123",
                ItemId = 1,
                ViewedAt = DateTime.UtcNow.AddDays(-1)
            };

            _recentlyViewedRepositoryMock
                .Setup(r => r.GetAsync("user-123", 1))
                .ReturnsAsync(existing);

            _recentlyViewedRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.TrackViewAsync("user-123", 1);

            // Assert
            existing.ViewedAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));

            _recentlyViewedRepositoryMock.Verify(r => r.Update(existing), Times.Once);
            _recentlyViewedRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
            _recentlyViewedRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserRecentlyViewedItem>()), Times.Never);
        }

        [Fact]
        public async Task TrackViewAsync_ShouldAddNewView_WhenItemDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            _recentlyViewedRepositoryMock
                .Setup(r => r.GetAsync("user-123", 1))
                .ReturnsAsync((UserRecentlyViewedItem?)null);

            _recentlyViewedRepositoryMock
                .Setup(r => r.GetCountByUserIdAsync("user-123"))
                .ReturnsAsync(3);

            _recentlyViewedRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<UserRecentlyViewedItem>()))
                .Returns(Task.CompletedTask);

            _recentlyViewedRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.TrackViewAsync("user-123", 1);

            // Assert
            _recentlyViewedRepositoryMock.Verify(r => r.AddAsync(It.Is<UserRecentlyViewedItem>(x =>
                x.UserId == "user-123" &&
                x.ItemId == 1
            )), Times.Once);

            _recentlyViewedRepositoryMock.Verify(r => r.DeleteOldestAsync(It.IsAny<string>()), Times.Never);
            _recentlyViewedRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task TrackViewAsync_ShouldDeleteOldest_WhenBufferIsFull()
        {
            // Arrange
            var service = CreateService();

            _recentlyViewedRepositoryMock
                .Setup(r => r.GetAsync("user-123", 1))
                .ReturnsAsync((UserRecentlyViewedItem?)null);

            _recentlyViewedRepositoryMock
                .Setup(r => r.GetCountByUserIdAsync("user-123"))
                .ReturnsAsync(15);

            _recentlyViewedRepositoryMock
                .Setup(r => r.DeleteOldestAsync("user-123"))
                .Returns(Task.CompletedTask);

            _recentlyViewedRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<UserRecentlyViewedItem>()))
                .Returns(Task.CompletedTask);

            _recentlyViewedRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.TrackViewAsync("user-123", 1);

            // Assert
            _recentlyViewedRepositoryMock.Verify(r => r.DeleteOldestAsync("user-123"), Times.Once);
            _recentlyViewedRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserRecentlyViewedItem>()), Times.Once);
            _recentlyViewedRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetRecentlyViewedAsync_ShouldReturnMappedItems()
        {
            // Arrange
            var service = CreateService();

            var item = CreateItem();

            var recentlyViewed = new List<UserRecentlyViewedItem>
            {
                new UserRecentlyViewedItem
                {
                    UserId = "user-123",
                    ItemId = item.Id,
                    Item = item,
                    ViewedAt = DateTime.UtcNow
                }
            };

            _recentlyViewedRepositoryMock
                .Setup(r => r.GetByUserIdAsync("user-123", 10))
                .ReturnsAsync(recentlyViewed);

            // Act
            var result = await service.GetRecentlyViewedAsync("user-123");

            // Assert
            result.Should().HaveCount(1);

            result[0].ItemId.Should().Be(item.Id);
            result[0].ItemTitle.Should().Be(item.Title);
            result[0].ItemSlug.Should().Be(item.Slug);
            result[0].ItemMainPhotoUrl.Should().Be("https://example.com/photo.jpg");
            result[0].PricePerDay.Should().Be(item.PricePerDay);
            result[0].IsFree.Should().BeFalse();
            result[0].IsAvailable.Should().BeTrue();
            result[0].OwnerName.Should().Be("Owner User");
        }

        [Fact]
        public async Task GetRecentlyViewedAsync_ShouldIgnoreDeletedItems()
        {
            // Arrange
            var service = CreateService();

            var item = CreateItem();
            item.IsDeleted = true;

            var recentlyViewed = new List<UserRecentlyViewedItem>
            {
                new UserRecentlyViewedItem
                {
                    UserId = "user-123",
                    ItemId = item.Id,
                    Item = item,
                    ViewedAt = DateTime.UtcNow
                }
            };

            _recentlyViewedRepositoryMock
                .Setup(r => r.GetByUserIdAsync("user-123", 10))
                .ReturnsAsync(recentlyViewed);

            // Act
            var result = await service.GetRecentlyViewedAsync("user-123");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetRecentlyViewedAsync_ShouldIgnoreNullItems()
        {
            // Arrange
            var service = CreateService();

            var recentlyViewed = new List<UserRecentlyViewedItem>
            {
                new UserRecentlyViewedItem
                {
                    UserId = "user-123",
                    ItemId = 1,
                    Item = null,
                    ViewedAt = DateTime.UtcNow
                }
            };

            _recentlyViewedRepositoryMock
                .Setup(r => r.GetByUserIdAsync("user-123", 10))
                .ReturnsAsync(recentlyViewed);

            // Act
            var result = await service.GetRecentlyViewedAsync("user-123");

            // Assert
            result.Should().BeEmpty();
        }
    }
}