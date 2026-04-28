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
    public class VerificationRequestControllerTests
    {
        private readonly Mock<IVerificationRequestService> _verificationServiceMock = new();

        private const string UserId = "user-123";
        private const string AdminId = "admin-123";

        private VerificationRequestController CreateController(bool isAdmin = false)
        {
            var controller = new VerificationRequestController(_verificationServiceMock.Object);

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
        public async Task SubmitRequest_ShouldPassCurrentUserIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController();

            var dto = new CreateVerificationRequestDto();

            var verificationRequest = new VerificationRequestDto();

            _verificationServiceMock
                .Setup(s => s.SubmitRequestAsync(UserId, dto))
                .ReturnsAsync(verificationRequest);

            // Act
            var response = await controller.SubmitRequest(dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<VerificationRequestDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(verificationRequest);
            apiResponse.Message.Should().Be("Verification request submitted successfully.");

            _verificationServiceMock.Verify(
                s => s.SubmitRequestAsync(UserId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task GetMyRequests_ShouldPassCurrentUserIdFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController();

            var filter = new VerificationRequestFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<VerificationRequestDto>
            {
                Items = new List<VerificationRequestDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _verificationServiceMock
                .Setup(s => s.GetMyRequestsAsync(UserId, filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetMyRequests(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<VerificationRequestDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _verificationServiceMock.Verify(
                s => s.GetMyRequestsAsync(UserId, filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task GetById_ShouldPassIsAdminFalse_WhenNormalUser()
        {
            // Arrange
            var controller = CreateController();

            var requestId = 1;

            var verificationRequest = new VerificationRequestDto();

            _verificationServiceMock
                .Setup(s => s.GetByIdAsync(requestId, UserId, false))
                .ReturnsAsync(verificationRequest);

            // Act
            var response = await controller.GetById(requestId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<VerificationRequestDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(verificationRequest);

            _verificationServiceMock.Verify(
                s => s.GetByIdAsync(requestId, UserId, false),
                Times.Once
            );
        }

        [Fact]
        public async Task GetById_ShouldPassIsAdminTrue_WhenAdmin()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var requestId = 1;

            var verificationRequest = new VerificationRequestDto();

            _verificationServiceMock
                .Setup(s => s.GetByIdAsync(requestId, AdminId, true))
                .ReturnsAsync(verificationRequest);

            // Act
            var response = await controller.GetById(requestId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<VerificationRequestDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(verificationRequest);

            _verificationServiceMock.Verify(
                s => s.GetByIdAsync(requestId, AdminId, true),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAll_ShouldPassFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var filter = new VerificationRequestFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<VerificationRequestDto>
            {
                Items = new List<VerificationRequestDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _verificationServiceMock
                .Setup(s => s.GetAllAsync(filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetAll(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<VerificationRequestDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _verificationServiceMock.Verify(
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

            var filter = new VerificationRequestFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<VerificationRequestDto>
            {
                Items = new List<VerificationRequestDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _verificationServiceMock
                .Setup(s => s.GetByUserIdAsync(targetUserId, filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetByUserId(targetUserId, filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<VerificationRequestDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _verificationServiceMock.Verify(
                s => s.GetByUserIdAsync(targetUserId, filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task Decide_ShouldPassRequestIdAdminIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var requestId = 1;
            var dto = new AdminDecideVerificationRequestDto();

            var verificationRequest = new VerificationRequestDto();

            _verificationServiceMock
                .Setup(s => s.DecideAsync(requestId, AdminId, dto))
                .ReturnsAsync(verificationRequest);

            // Act
            var response = await controller.Decide(requestId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<VerificationRequestDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(verificationRequest);
            apiResponse.Message.Should().Be("Verification decision recorded.");

            _verificationServiceMock.Verify(
                s => s.DecideAsync(requestId, AdminId, dto),
                Times.Once
            );
        }
    }
}