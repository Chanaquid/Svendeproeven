using System.Security.Claims;
using backend.Common;
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
    public class ItemReviewControllerTests
    {
        private readonly Mock<IItemReviewService> _itemReviewServiceMock = new();

        private const string UserId = "user-123";
        private const string AdminId = "admin-123";

        private ItemReviewController CreateController(bool isAdmin = false)
        {
            var controller = new ItemReviewController(_itemReviewServiceMock.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, isAdmin ? AdminId : UserId)
            };

            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, Roles.Admin));
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
        public async Task GetByItem_ShouldPassItemIdCurrentUserIdFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController();

            var itemId = 1;
            var filter = new ItemReviewFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<ItemReviewDto>
            {
                Items = new List<ItemReviewDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _itemReviewServiceMock
                .Setup(s => s.GetByItemIdAsync(itemId, UserId, filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetByItem(itemId, filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<ItemReviewDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _itemReviewServiceMock.Verify(
                s => s.GetByItemIdAsync(itemId, UserId, filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task CreateReview_ShouldSetItemIdAndPassCurrentUserIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController();

            var itemId = 1;

            var dto = new CreateItemReviewDto
            {
                Rating = 5,
                Comment = "Good item"
            };

            var review = new ItemReviewDto
            {
                Id = 10
            };

            _itemReviewServiceMock
                .Setup(s => s.CreateItemReviewAsync(UserId, dto))
                .ReturnsAsync(review);

            // Act
            var response = await controller.CreateReview(itemId, dto);

            // Assert
            dto.ItemId.Should().Be(itemId);

            var createdResult = response.Result.Should().BeOfType<CreatedAtActionResult>().Subject;

            createdResult.ActionName.Should().Be(nameof(ItemReviewController.GetByItem));
            createdResult.RouteValues!["itemId"].Should().Be(itemId);

            var apiResponse = createdResult.Value
                .Should()
                .BeOfType<ApiResponse<ItemReviewDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(review);
            apiResponse.Message.Should().Be("Review submitted successfully.");

            _itemReviewServiceMock.Verify(
                s => s.CreateItemReviewAsync(UserId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task EditReview_ShouldPassReviewIdCurrentUserIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController();

            var itemId = 1;
            var reviewId = 10;

            var dto = new UpdateItemReviewDto
            {
                Rating = 4,
                Comment = "Updated review"
            };

            var review = new ItemReviewDto
            {
                Id = reviewId
            };

            _itemReviewServiceMock
                .Setup(s => s.EditItemReviewAsync(reviewId, UserId, dto))
                .ReturnsAsync(review);

            // Act
            var response = await controller.EditReview(itemId, reviewId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<ItemReviewDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(review);
            apiResponse.Message.Should().Be("Review updated successfully.");

            _itemReviewServiceMock.Verify(
                s => s.EditItemReviewAsync(reviewId, UserId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task DeleteReview_ShouldPassReviewIdAndAdminIdToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var itemId = 1;
            var reviewId = 10;

            _itemReviewServiceMock
                .Setup(s => s.DeleteItemReviewAsync(reviewId, AdminId))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.DeleteReview(itemId, reviewId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Message.Should().Be("Review deleted successfully.");

            _itemReviewServiceMock.Verify(
                s => s.DeleteItemReviewAsync(reviewId, AdminId),
                Times.Once
            );
        }

        [Fact]
        public async Task AdminCreateReview_ShouldBuildCreateDtoAndPassAdminIdToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var itemId = 1;

            var dto = new AdminCreateItemReviewDto
            {
                Rating = 5,
                Comment = "Admin review"
            };

            var review = new ItemReviewDto
            {
                Id = 10
            };

            _itemReviewServiceMock
                .Setup(s => s.CreateItemReviewAsync(
                    AdminId,
                    It.Is<CreateItemReviewDto>(createDto =>
                        createDto.ItemId == itemId &&
                        createDto.LoanId == null &&
                        createDto.Rating == dto.Rating &&
                        createDto.Comment == dto.Comment)))
                .ReturnsAsync(review);

            // Act
            var response = await controller.AdminCreateReview(itemId, dto);

            // Assert
            var createdResult = response.Result.Should().BeOfType<CreatedAtActionResult>().Subject;

            createdResult.ActionName.Should().Be(nameof(ItemReviewController.GetByItem));
            createdResult.RouteValues!["itemId"].Should().Be(itemId);

            var apiResponse = createdResult.Value
                .Should()
                .BeOfType<ApiResponse<ItemReviewDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(review);
            apiResponse.Message.Should().Be("Admin review submitted successfully.");

            _itemReviewServiceMock.Verify(
                s => s.CreateItemReviewAsync(
                    AdminId,
                    It.Is<CreateItemReviewDto>(createDto =>
                        createDto.ItemId == itemId &&
                        createDto.LoanId == null &&
                        createDto.Rating == dto.Rating &&
                        createDto.Comment == dto.Comment)),
                Times.Once
            );
        }
    }
}