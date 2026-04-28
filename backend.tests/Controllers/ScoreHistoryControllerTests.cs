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
    public class ScoreHistoryControllerTests
    {
        private readonly Mock<IScoreHistoryService> _scoreHistoryServiceMock = new();

        private const string UserId = "user-123";
        private const string AdminId = "admin-123";

        private ScoreHistoryController CreateController(bool isAdmin = false)
        {
            var controller = new ScoreHistoryController(_scoreHistoryServiceMock.Object);

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
        public async Task GetMyHistory_ShouldPassCurrentUserIdFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController();

            var filter = new ScoreHistoryFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<ScoreHistoryDto>
            {
                Items = new List<ScoreHistoryDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _scoreHistoryServiceMock
                .Setup(s => s.GetMyHistoryAsync(UserId, filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetMyHistory(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<ScoreHistoryDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _scoreHistoryServiceMock.Verify(
                s => s.GetMyHistoryAsync(UserId, filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task GetMyScoreSummary_ShouldPassCurrentUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var summary = new UserScoreSummaryDto();

            _scoreHistoryServiceMock
                .Setup(s => s.GetMyScoreSummaryAsync(UserId))
                .ReturnsAsync(summary);

            // Act
            var response = await controller.GetMyScoreSummary();

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<UserScoreSummaryDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(summary);

            _scoreHistoryServiceMock.Verify(
                s => s.GetMyScoreSummaryAsync(UserId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAll_ShouldPassFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var filter = new ScoreHistoryFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<ScoreHistoryDto>
            {
                Items = new List<ScoreHistoryDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _scoreHistoryServiceMock
                .Setup(s => s.GetAllAsync(filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetAll(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<ScoreHistoryDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _scoreHistoryServiceMock.Verify(
                s => s.GetAllAsync(filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task GetByUserId_ShouldPassTargetUserIdFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var targetUserId = "target-user-123";

            var filter = new ScoreHistoryFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<ScoreHistoryDto>
            {
                Items = new List<ScoreHistoryDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _scoreHistoryServiceMock
                .Setup(s => s.GetByUserIdAsync(targetUserId, filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetByUserId(targetUserId, filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<ScoreHistoryDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _scoreHistoryServiceMock.Verify(
                s => s.GetByUserIdAsync(targetUserId, filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task GetScoreSummaryByUserId_ShouldPassTargetUserIdToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var targetUserId = "target-user-123";

            var summary = new UserScoreSummaryDto();

            _scoreHistoryServiceMock
                .Setup(s => s.GetScoreSummaryByUserIdAsync(targetUserId))
                .ReturnsAsync(summary);

            // Act
            var response = await controller.GetScoreSummaryByUserId(targetUserId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<UserScoreSummaryDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(summary);

            _scoreHistoryServiceMock.Verify(
                s => s.GetScoreSummaryByUserIdAsync(targetUserId),
                Times.Once
            );
        }

        [Fact]
        public async Task AdminAdjustScore_ShouldPassAdminIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var dto = new AdminAdjustScoreDto();

            _scoreHistoryServiceMock
                .Setup(s => s.AdminAdjustScoreAsync(AdminId, dto))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.AdminAdjustScore(dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Message.Should().Be("Score adjusted successfully.");

            _scoreHistoryServiceMock.Verify(
                s => s.AdminAdjustScoreAsync(AdminId, dto),
                Times.Once
            );
        }
    }
}