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
    public class UserBanHistoryControllerTests
    {
        private readonly Mock<IUserBanHistoryService> _banHistoryServiceMock = new();

        private const string AdminId = "admin-123";

        private UserBanHistoryController CreateController()
        {
            var controller = new UserBanHistoryController(_banHistoryServiceMock.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, AdminId),
                new Claim(ClaimTypes.Role, "Admin")
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
        public async Task GetAll_ShouldPassFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController();

            var filter = new UserBanHistoryFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<UserBanHistoryDto>
            {
                Items = new List<UserBanHistoryDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _banHistoryServiceMock
                .Setup(s => s.GetAllAsync(filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetAll(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<UserBanHistoryDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _banHistoryServiceMock.Verify(
                s => s.GetAllAsync(filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task GetById_ShouldPassIdToService()
        {
            // Arrange
            var controller = CreateController();

            var banHistoryId = 1;

            var banHistory = new UserBanHistoryDto
            {
                Id = banHistoryId
            };

            _banHistoryServiceMock
                .Setup(s => s.GetByIdAsync(banHistoryId))
                .ReturnsAsync(banHistory);

            // Act
            var response = await controller.GetById(banHistoryId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<UserBanHistoryDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(banHistory);

            _banHistoryServiceMock.Verify(
                s => s.GetByIdAsync(banHistoryId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetByUserId_ShouldPassUserIdFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController();

            var targetUserId = "user-123";

            var filter = new UserBanHistoryFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<UserBanHistoryDto>
            {
                Items = new List<UserBanHistoryDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _banHistoryServiceMock
                .Setup(s => s.GetByUserIdAsync(targetUserId, filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetByUserId(targetUserId, filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<UserBanHistoryDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _banHistoryServiceMock.Verify(
                s => s.GetByUserIdAsync(targetUserId, filter, request),
                Times.Once
            );
        }
    }
}