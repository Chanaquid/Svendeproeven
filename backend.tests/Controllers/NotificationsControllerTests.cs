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
    public class NotificationControllerTests
    {
        private readonly Mock<INotificationService> _notificationServiceMock = new();

        private const string UserId = "user-123";

        private NotificationController CreateController()
        {
            var controller = new NotificationController(_notificationServiceMock.Object);

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
        public async Task GetSummary_ShouldPassCurrentUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var summary = new NotificationSummaryDto();

            _notificationServiceMock
                .Setup(s => s.GetSummaryAsync(UserId))
                .ReturnsAsync(summary);

            // Act
            var response = await controller.GetSummary();

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<NotificationSummaryDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(summary);

            _notificationServiceMock.Verify(
                s => s.GetSummaryAsync(UserId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAll_ShouldPassCurrentUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var filter = new NotificationFilter();
            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<NotificationDto>
            {
                Items = new List<NotificationDto> { new NotificationDto() },
                Page = 1,
                PageSize = 10,
                TotalCount = 1
            };

            _notificationServiceMock
                .Setup(s => s.GetAllAsync(UserId, filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetAll(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<NotificationDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _notificationServiceMock.Verify(
                s => s.GetAllAsync(UserId, filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task MarkAsRead_ShouldPassNotificationIdAndCurrentUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var notificationId = 1;

            _notificationServiceMock
                .Setup(s => s.MarkAsReadAsync(notificationId, UserId))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.MarkAsRead(notificationId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Message.Should().Be("Notification marked as read.");

            _notificationServiceMock.Verify(
                s => s.MarkAsReadAsync(notificationId, UserId),
                Times.Once
            );
        }

        [Fact]
        public async Task MarkMultipleAsRead_ShouldPassNotificationIdsAndCurrentUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var dto = new MarkMultipleNotificationsReadDto
            {
                NotificationIds = new List<int> { 1, 2, 3 }
            };

            _notificationServiceMock
                .Setup(s => s.MarkMultipleAsReadAsync(dto.NotificationIds, UserId))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.MarkMultipleAsRead(dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Message.Should().Be("Notifications marked as read.");

            _notificationServiceMock.Verify(
                s => s.MarkMultipleAsReadAsync(dto.NotificationIds, UserId),
                Times.Once
            );
        }

        [Fact]
        public async Task MarkAllAsRead_ShouldPassCurrentUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            _notificationServiceMock
                .Setup(s => s.MarkAllAsReadAsync(UserId))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.MarkAllAsRead();

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Message.Should().Be("All notifications marked as read.");

            _notificationServiceMock.Verify(
                s => s.MarkAllAsReadAsync(UserId),
                Times.Once
            );
        }

        [Fact]
        public async Task Delete_ShouldPassNotificationIdAndCurrentUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var notificationId = 1;

            _notificationServiceMock
                .Setup(s => s.DeleteAsync(notificationId, UserId))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.Delete(notificationId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Message.Should().Be("Notification deleted.");

            _notificationServiceMock.Verify(
                s => s.DeleteAsync(notificationId, UserId),
                Times.Once
            );
        }

        [Fact]
        public async Task DeleteAll_ShouldPassCurrentUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            _notificationServiceMock
                .Setup(s => s.DeleteAllAsync(UserId))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.DeleteAll();

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Message.Should().Be("All notifications deleted.");

            _notificationServiceMock.Verify(
                s => s.DeleteAllAsync(UserId),
                Times.Once
            );
        }
    }
}