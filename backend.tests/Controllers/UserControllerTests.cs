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
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _userServiceMock = new();

        private const string UserId = "user-123";

        private UserController CreateController()
        {
            var controller = new UserController(_userServiceMock.Object);

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
        public async Task GetTotalUsersCount_ShouldReturnTotalUsersCount()
        {
            // Arrange
            var controller = CreateController();

            var count = 25;

            _userServiceMock
                .Setup(s => s.GetTotalUsersCountAsync())
                .ReturnsAsync(count);

            // Act
            var response = await controller.GetTotalUsersCount();

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<int>>()
                .Subject;

            apiResponse.Data.Should().Be(count);

            _userServiceMock.Verify(
                s => s.GetTotalUsersCountAsync(),
                Times.Once
            );
        }

        [Fact]
        public async Task GetMyProfile_ShouldPassCurrentUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var profile = new UserProfileDto();

            _userServiceMock
                .Setup(s => s.GetProfileAsync(UserId))
                .ReturnsAsync(profile);

            // Act
            var response = await controller.GetMyProfile();

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<UserProfileDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(profile);

            _userServiceMock.Verify(
                s => s.GetProfileAsync(UserId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetPublicProfile_ShouldPassTargetUserIdAndCurrentUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var targetUserId = "user-456";

            var publicProfile = new UserPublicProfileDto();

            _userServiceMock
                .Setup(s => s.GetPublicProfileAsync(targetUserId, UserId))
                .ReturnsAsync(publicProfile);

            // Act
            var response = await controller.GetPublicProfile(targetUserId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<UserPublicProfileDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(publicProfile);

            _userServiceMock.Verify(
                s => s.GetPublicProfileAsync(targetUserId, UserId),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateProfile_ShouldPassCurrentUserIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController();

            var dto = new UpdateProfileDto
            {
                FullName = "Updated Name"
            };

            var updatedProfile = new UserProfileDto();

            _userServiceMock
                .Setup(s => s.UpdateProfileAsync(UserId, dto))
                .ReturnsAsync(updatedProfile);

            // Act
            var response = await controller.UpdateProfile(dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<UserProfileDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(updatedProfile);
            apiResponse.Message.Should().Be("Profile updated successfully.");

            _userServiceMock.Verify(
                s => s.UpdateProfileAsync(UserId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateAvatar_ShouldReturnBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            var controller = CreateController();

            controller.ModelState.AddModelError("AvatarUrl", "Invalid avatar URL");

            var dto = new UpdateAvatarDto
            {
                AvatarUrl = "not-valid"
            };

            // Act
            var response = await controller.UpdateAvatar(dto);

            // Assert
            var badRequestResult = response.Result.Should().BeOfType<BadRequestObjectResult>().Subject;

            var apiResponse = badRequestResult.Value
                .Should()
                .BeOfType<ApiResponse<UserProfileDto>>()
                .Subject;

            apiResponse.Success.Should().BeFalse();
            apiResponse.Message.Should().Be("Invalid avatar URL");

            _userServiceMock.Verify(
                s => s.UpdateProfileAsync(It.IsAny<string>(), It.IsAny<UpdateProfileDto>()),
                Times.Never
            );
        }

        [Fact]
        public async Task UpdateAvatar_ShouldBuildUpdateProfileDtoAndPassCurrentUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var dto = new UpdateAvatarDto
            {
                AvatarUrl = "https://example.com/avatar.png"
            };

            var updatedProfile = new UserProfileDto();

            _userServiceMock
                .Setup(s => s.UpdateProfileAsync(
                    UserId,
                    It.Is<UpdateProfileDto>(updateDto =>
                        updateDto.AvatarUrl == dto.AvatarUrl)))
                .ReturnsAsync(updatedProfile);

            // Act
            var response = await controller.UpdateAvatar(dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<UserProfileDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(updatedProfile);
            apiResponse.Message.Should().Be("Avatar updated successfully.");

            _userServiceMock.Verify(
                s => s.UpdateProfileAsync(
                    UserId,
                    It.Is<UpdateProfileDto>(updateDto =>
                        updateDto.AvatarUrl == dto.AvatarUrl)),
                Times.Once
            );
        }

        [Fact]
        public async Task DeleteAccount_ShouldPassCurrentUserIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController();

            var dto = new DeleteAccountDto();

            _userServiceMock
                .Setup(s => s.DeleteAccountAsync(UserId, dto))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.DeleteAccount(dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Message.Should().Be("Your account has been successfully deleted.");

            _userServiceMock.Verify(
                s => s.DeleteAccountAsync(UserId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task SearchUsers_ShouldPassFilterRequestAndCurrentUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var filter = new UserFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<UserProfileDto>
            {
                Items = new List<UserProfileDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _userServiceMock
                .Setup(s => s.SearchUsersAsync(filter, request, UserId))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.SearchUsers(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<UserProfileDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _userServiceMock.Verify(
                s => s.SearchUsersAsync(filter, request, UserId),
                Times.Once
            );
        }
    }
}