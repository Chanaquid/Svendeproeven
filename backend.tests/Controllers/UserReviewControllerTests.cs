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
    public class UserReviewControllerTests
    {
        private readonly Mock<IUserReviewService> _reviewServiceMock = new();

        private const string UserId = "user-123";
        private const string AdminId = "admin-123";

        private UserReviewController CreateController(bool isAdmin = false)
        {
            var controller = new UserReviewController(_reviewServiceMock.Object);

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
        public async Task CreateReview_ShouldPassCurrentUserIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController();

            var dto = new CreateUserReviewDto();

            var review = new UserReviewDto();

            _reviewServiceMock
                .Setup(s => s.CreateReviewAsync(UserId, dto))
                .ReturnsAsync(review);

            // Act
            var response = await controller.CreateReview(dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<UserReviewDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(review);
            apiResponse.Message.Should().Be("Review submitted successfully.");

            _reviewServiceMock.Verify(
                s => s.CreateReviewAsync(UserId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateReview_ShouldPassReviewIdCurrentUserIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController();

            var reviewId = 1;
            var dto = new UpdateUserReviewDto();

            var review = new UserReviewDto();

            _reviewServiceMock
                .Setup(s => s.UpdateReviewAsync(reviewId, UserId, dto))
                .ReturnsAsync(review);

            // Act
            var response = await controller.UpdateReview(reviewId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<UserReviewDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(review);
            apiResponse.Message.Should().Be("Review updated successfully.");

            _reviewServiceMock.Verify(
                s => s.UpdateReviewAsync(reviewId, UserId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task GetById_ShouldPassIsAdminFalse_WhenNormalUser()
        {
            // Arrange
            var controller = CreateController();

            var reviewId = 1;

            var review = new UserReviewDto();

            _reviewServiceMock
                .Setup(s => s.GetByIdAsync(reviewId, UserId, false))
                .ReturnsAsync(review);

            // Act
            var response = await controller.GetById(reviewId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<UserReviewDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(review);

            _reviewServiceMock.Verify(
                s => s.GetByIdAsync(reviewId, UserId, false),
                Times.Once
            );
        }

        [Fact]
        public async Task GetById_ShouldPassIsAdminTrue_WhenAdmin()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var reviewId = 1;

            var review = new UserReviewDto();

            _reviewServiceMock
                .Setup(s => s.GetByIdAsync(reviewId, AdminId, true))
                .ReturnsAsync(review);

            // Act
            var response = await controller.GetById(reviewId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<UserReviewDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(review);

            _reviewServiceMock.Verify(
                s => s.GetByIdAsync(reviewId, AdminId, true),
                Times.Once
            );
        }

        [Fact]
        public async Task GetReviewsForUser_ShouldPassUserIdFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController();

            var targetUserId = "target-user-123";

            var filter = new UserReviewFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<UserReviewListDto>
            {
                Items = new List<UserReviewListDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _reviewServiceMock
                .Setup(s => s.GetReviewsForUserAsync(targetUserId, filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetReviewsForUser(targetUserId, filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<UserReviewListDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _reviewServiceMock.Verify(
                s => s.GetReviewsForUserAsync(targetUserId, filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task GetRatingSummary_ShouldPassUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var targetUserId = "target-user-123";

            var summary = new UserRatingSummaryDto();

            _reviewServiceMock
                .Setup(s => s.GetRatingSummaryAsync(targetUserId))
                .ReturnsAsync(summary);

            // Act
            var response = await controller.GetRatingSummary(targetUserId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<UserRatingSummaryDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(summary);

            _reviewServiceMock.Verify(
                s => s.GetRatingSummaryAsync(targetUserId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetMyGivenReviews_ShouldPassCurrentUserIdFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController();

            var filter = new UserReviewFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<UserReviewListDto>
            {
                Items = new List<UserReviewListDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _reviewServiceMock
                .Setup(s => s.GetMyGivenReviewsAsync(UserId, filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetMyGivenReviews(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<UserReviewListDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _reviewServiceMock.Verify(
                s => s.GetMyGivenReviewsAsync(UserId, filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAll_ShouldPassFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var filter = new UserReviewFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<UserReviewDto>
            {
                Items = new List<UserReviewDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _reviewServiceMock
                .Setup(s => s.GetAllAsync(filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetAll(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<UserReviewDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _reviewServiceMock.Verify(
                s => s.GetAllAsync(filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task AdminCreateReview_ShouldPassAdminIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var dto = new AdminCreateUserReviewDto();

            var review = new UserReviewDto();

            _reviewServiceMock
                .Setup(s => s.AdminCreateReviewAsync(AdminId, dto))
                .ReturnsAsync(review);

            // Act
            var response = await controller.AdminCreateReview(dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<UserReviewDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(review);
            apiResponse.Message.Should().Be("Admin review submitted successfully.");

            _reviewServiceMock.Verify(
                s => s.AdminCreateReviewAsync(AdminId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task AdminDeleteReview_ShouldPassReviewIdToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var reviewId = 1;

            _reviewServiceMock
                .Setup(s => s.AdminDeleteReviewAsync(reviewId))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.AdminDeleteReview(reviewId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Message.Should().Be("Review deleted successfully.");

            _reviewServiceMock.Verify(
                s => s.AdminDeleteReviewAsync(reviewId),
                Times.Once
            );
        }
    }
}