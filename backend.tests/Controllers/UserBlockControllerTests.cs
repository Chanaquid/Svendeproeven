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
    public class UserBlockControllerTests
    {
        private readonly Mock<IUserBlockService> _blockServiceMock = new();

        private const string UserId = "user-123";
        private const string AdminId = "admin-123";

        private UserBlockController CreateController(bool isAdmin = false)
        {
            var controller = new UserBlockController(_blockServiceMock.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, isAdmin ? AdminId : UserId)
            };

            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

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
        public async Task BlockUser_ShouldPassCurrentUserIdAndTargetUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var targetUserId = "user-456";

            _blockServiceMock
                .Setup(s => s.BlockUserAsync(UserId, targetUserId))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.BlockUser(targetUserId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Message.Should().Be("User blocked successfully.");

            _blockServiceMock.Verify(
                s => s.BlockUserAsync(UserId, targetUserId),
                Times.Once
            );
        }

        [Fact]
        public async Task UnblockUser_ShouldPassCurrentUserIdAndTargetUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var targetUserId = "user-456";

            _blockServiceMock
                .Setup(s => s.UnblockUserAsync(UserId, targetUserId))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.UnblockUser(targetUserId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Message.Should().Be("User unblocked successfully.");

            _blockServiceMock.Verify(
                s => s.UnblockUserAsync(UserId, targetUserId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetMyBlocks_ShouldPassCurrentUserIdFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController();

            var filter = new UserBlockFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<UserBlockListDto>
            {
                Items = new List<UserBlockListDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _blockServiceMock
                .Setup(s => s.GetMyBlocksAsync(UserId, filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetMyBlocks(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<UserBlockListDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _blockServiceMock.Verify(
                s => s.GetMyBlocksAsync(UserId, filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task CheckBlockStatus_ShouldPassCurrentUserIdAndTargetUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var targetUserId = "user-456";

            _blockServiceMock
                .Setup(s => s.IsBlockedAsync(UserId, targetUserId))
                .ReturnsAsync(true);

            // Act
            var response = await controller.CheckBlockStatus(targetUserId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<bool>>()
                .Subject;

            apiResponse.Data.Should().BeTrue();

            _blockServiceMock.Verify(
                s => s.IsBlockedAsync(UserId, targetUserId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAll_ShouldPassFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var filter = new UserBlockFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<UserBlockDto>
            {
                Items = new List<UserBlockDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _blockServiceMock
                .Setup(s => s.GetAllAsync(filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetAll(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<UserBlockDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _blockServiceMock.Verify(
                s => s.GetAllAsync(filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task AdminUnblock_ShouldPassBlockerIdAndBlockedIdToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var blockerId = "user-123";
            var blockedId = "user-456";

            _blockServiceMock
                .Setup(s => s.AdminUnblockAsync(blockerId, blockedId))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.AdminUnblock(blockerId, blockedId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Message.Should().Be("Block removed successfully.");

            _blockServiceMock.Verify(
                s => s.AdminUnblockAsync(blockerId, blockedId),
                Times.Once
            );
        }
    }
}