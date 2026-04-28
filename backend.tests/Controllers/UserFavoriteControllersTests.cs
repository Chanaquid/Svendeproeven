using System.Security.Claims;
using backend.Controllers;
using backend.Dtos;
using backend.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace backend.Tests.Controllers
{
    public class UserFavoriteControllerTests
    {
        private readonly Mock<IUserFavoriteService> _favoriteServiceMock = new();

        private const string UserId = "user-123";

        private UserFavoriteController CreateController()
        {
            var controller = new UserFavoriteController(_favoriteServiceMock.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, UserId)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user
                }
            };

            return controller;
        }

        [Fact]
        public async Task GetMyFavorites_ShouldPassCurrentUserIdAndRequestToService()
        {
            // Arrange
            var controller = CreateController();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<UserFavoriteItemListDto>
            {
                Items = new List<UserFavoriteItemListDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _favoriteServiceMock
                .Setup(s => s.GetFavoritesAsync(UserId, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetMyFavorites(request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<UserFavoriteItemListDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);
            apiResponse.Message.Should().Be("Favorites retrieved successfully");

            _favoriteServiceMock.Verify(
                s => s.GetFavoritesAsync(UserId, request),
                Times.Once
            );
        }

        [Fact]
        public async Task Toggle_ShouldReturnAddedMessage_WhenItemIsFavorited()
        {
            // Arrange
            var controller = CreateController();

            var itemId = 10;
            var notify = true;

            _favoriteServiceMock
                .Setup(s => s.ToggleFavoriteAsync(UserId, itemId, notify))
                .ReturnsAsync(true);

            // Act
            var response = await controller.Toggle(itemId, notify);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<FavoriteToggleResultDto>>()
                .Subject;

            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.ItemId.Should().Be(itemId);
            apiResponse.Data.IsFavorited.Should().BeTrue();
            apiResponse.Message.Should().Be("Item added to favorites");

            _favoriteServiceMock.Verify(
                s => s.ToggleFavoriteAsync(UserId, itemId, notify),
                Times.Once
            );
        }

        [Fact]
        public async Task Toggle_ShouldReturnRemovedMessage_WhenItemIsNotFavorited()
        {
            // Arrange
            var controller = CreateController();

            var itemId = 10;
            var notify = false;

            _favoriteServiceMock
                .Setup(s => s.ToggleFavoriteAsync(UserId, itemId, notify))
                .ReturnsAsync(false);

            // Act
            var response = await controller.Toggle(itemId, notify);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<FavoriteToggleResultDto>>()
                .Subject;

            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.ItemId.Should().Be(itemId);
            apiResponse.Data.IsFavorited.Should().BeFalse();
            apiResponse.Message.Should().Be("Item removed from favorites");

            _favoriteServiceMock.Verify(
                s => s.ToggleFavoriteAsync(UserId, itemId, notify),
                Times.Once
            );
        }

        [Fact]
        public async Task GetStatus_ShouldReturnFavoriteStatus()
        {
            // Arrange
            var controller = CreateController();

            var itemId = 10;

            _favoriteServiceMock
                .Setup(s => s.IsFavoritedAsync(UserId, itemId))
                .ReturnsAsync(true);

            // Act
            var response = await controller.GetStatus(itemId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<FavoriteStatusDto>>()
                .Subject;

            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.ItemId.Should().Be(itemId);
            apiResponse.Data.IsFavorited.Should().BeTrue();

            _favoriteServiceMock.Verify(
                s => s.IsFavoritedAsync(UserId, itemId),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateNotifyPreference_ShouldPassCurrentUserIdItemIdAndNotifyToService()
        {
            // Arrange
            var controller = CreateController();

            var itemId = 10;

            var dto = new UpdateNotifyPreferenceDto
            {
                Notify = true
            };

            _favoriteServiceMock
                .Setup(s => s.UpdateNotifyPreferenceAsync(UserId, itemId, dto.Notify))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.UpdateNotifyPreference(itemId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<NotifyPreferenceResultDto>>()
                .Subject;

            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.ItemId.Should().Be(itemId);
            apiResponse.Data.NotifyWhenAvailable.Should().Be(dto.Notify);
            apiResponse.Message.Should().Be("Notification preference updated");

            _favoriteServiceMock.Verify(
                s => s.UpdateNotifyPreferenceAsync(UserId, itemId, dto.Notify),
                Times.Once
            );
        }
    }
}