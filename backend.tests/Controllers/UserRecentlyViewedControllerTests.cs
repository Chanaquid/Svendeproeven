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
    public class UserRecentlyViewedControllerTests
    {
        private readonly Mock<IUserRecentlyViewedService> _recentlyViewedServiceMock = new();

        private const string UserId = "user-123";

        private UserRecentlyViewedController CreateController()
        {
            var controller = new UserRecentlyViewedController(_recentlyViewedServiceMock.Object);

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
        public async Task GetRecentlyViewed_ShouldPassCurrentUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var recentlyViewedItems = new List<UserRecentlyViewedItemDto>
            {
                new UserRecentlyViewedItemDto()
            };

            _recentlyViewedServiceMock
                .Setup(s => s.GetRecentlyViewedAsync(UserId))
                .ReturnsAsync(recentlyViewedItems);

            // Act
            var response = await controller.GetRecentlyViewed();

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<List<UserRecentlyViewedItemDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(recentlyViewedItems);

            _recentlyViewedServiceMock.Verify(
                s => s.GetRecentlyViewedAsync(UserId),
                Times.Once
            );
        }

        [Fact]
        public async Task TrackView_ShouldPassCurrentUserIdAndItemIdToService()
        {
            // Arrange
            var controller = CreateController();

            var itemId = 10;

            _recentlyViewedServiceMock
                .Setup(s => s.TrackViewAsync(UserId, itemId))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.TrackView(itemId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Success.Should().BeTrue();

            _recentlyViewedServiceMock.Verify(
                s => s.TrackViewAsync(UserId, itemId),
                Times.Once
            );
        }
    }
}