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
    public class SupportControllerTests
    {
        private readonly Mock<ISupportService> _supportServiceMock = new();

        private const string UserId = "user-123";
        private const string AdminId = "admin-123";

        private SupportController CreateController(bool isAdmin = false)
        {
            var controller = new SupportController(_supportServiceMock.Object);

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
        public async Task CreateThread_ShouldPassCurrentUserIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController();

            var dto = new CreateSupportThreadDto();

            var thread = new SupportThreadDto();

            _supportServiceMock
                .Setup(s => s.CreateThreadAsync(UserId, dto))
                .ReturnsAsync(thread);

            // Act
            var response = await controller.CreateThread(dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<SupportThreadDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(thread);
            apiResponse.Message.Should().Be("Support thread created successfully.");

            _supportServiceMock.Verify(
                s => s.CreateThreadAsync(UserId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task GetMyThreads_ShouldPassCurrentUserIdFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController();

            var filter = new SupportThreadFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<SupportThreadListDto>
            {
                Items = new List<SupportThreadListDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _supportServiceMock
                .Setup(s => s.GetMyThreadsAsync(UserId, filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetMyThreads(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<SupportThreadListDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _supportServiceMock.Verify(
                s => s.GetMyThreadsAsync(UserId, filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task GetById_ShouldPassIsAdminFalse_WhenNormalUser()
        {
            // Arrange
            var controller = CreateController();

            var threadId = 1;

            var thread = new SupportThreadDto();

            _supportServiceMock
                .Setup(s => s.GetThreadByIdAsync(threadId, UserId, false))
                .ReturnsAsync(thread);

            // Act
            var response = await controller.GetById(threadId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<SupportThreadDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(thread);

            _supportServiceMock.Verify(
                s => s.GetThreadByIdAsync(threadId, UserId, false),
                Times.Once
            );
        }

        [Fact]
        public async Task GetById_ShouldPassIsAdminTrue_WhenAdmin()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var threadId = 1;

            var thread = new SupportThreadDto();

            _supportServiceMock
                .Setup(s => s.GetThreadByIdAsync(threadId, AdminId, true))
                .ReturnsAsync(thread);

            // Act
            var response = await controller.GetById(threadId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<SupportThreadDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(thread);

            _supportServiceMock.Verify(
                s => s.GetThreadByIdAsync(threadId, AdminId, true),
                Times.Once
            );
        }

        [Fact]
        public async Task SendMessage_ShouldPassUserIdDtoAndIsAdminFalse_WhenNormalUser()
        {
            // Arrange
            var controller = CreateController();

            var threadId = 1;
            var dto = new SendSupportMessageDto();

            var message = new SupportMessageDto();

            _supportServiceMock
                .Setup(s => s.SendMessageAsync(threadId, UserId, dto, false))
                .ReturnsAsync(message);

            // Act
            var response = await controller.SendMessage(threadId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<SupportMessageDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(message);

            _supportServiceMock.Verify(
                s => s.SendMessageAsync(threadId, UserId, dto, false),
                Times.Once
            );
        }

        [Fact]
        public async Task SendMessage_ShouldPassIsAdminTrue_WhenAdmin()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var threadId = 1;
            var dto = new SendSupportMessageDto();

            var message = new SupportMessageDto();

            _supportServiceMock
                .Setup(s => s.SendMessageAsync(threadId, AdminId, dto, true))
                .ReturnsAsync(message);

            // Act
            var response = await controller.SendMessage(threadId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<SupportMessageDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(message);

            _supportServiceMock.Verify(
                s => s.SendMessageAsync(threadId, AdminId, dto, true),
                Times.Once
            );
        }

        [Fact]
        public async Task CloseThread_ShouldPassThreadIdUserIdAndIsAdminFalse_WhenNormalUser()
        {
            // Arrange
            var controller = CreateController();

            var threadId = 1;

            _supportServiceMock
                .Setup(s => s.CloseThreadAsync(threadId, UserId, false))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.CloseThread(threadId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Message.Should().Be("Support thread closed.");

            _supportServiceMock.Verify(
                s => s.CloseThreadAsync(threadId, UserId, false),
                Times.Once
            );
        }

        [Fact]
        public async Task MarkAsRead_ShouldPassThreadIdUserIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController();

            var threadId = 1;
            var dto = new MarkSupportMessagesReadDto();

            _supportServiceMock
                .Setup(s => s.MarkMessagesAsReadAsync(threadId, UserId, dto))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.MarkAsRead(threadId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Success.Should().BeTrue();

            _supportServiceMock.Verify(
                s => s.MarkMessagesAsReadAsync(threadId, UserId, dto),
                Times.Once
            );
        }

        
        [Fact]
        public async Task GetAllThreads_ShouldPassFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var filter = new SupportThreadFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<SupportThreadListDto>
            {
                Items = new List<SupportThreadListDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _supportServiceMock
                .Setup(s => s.GetAllThreadsAsync(filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetAllThreads(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<SupportThreadListDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _supportServiceMock.Verify(
                s => s.GetAllThreadsAsync(filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task AdminCreateThread_ShouldPassAdminIdTargetUserIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var targetUserId = "target-user-123";
            var dto = new CreateSupportThreadDto();

            var thread = new SupportThreadDto();

            _supportServiceMock
                .Setup(s => s.AdminCreateThreadAsync(AdminId, targetUserId, dto))
                .ReturnsAsync(thread);

            // Act
            var response = await controller.AdminCreateThread(targetUserId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<SupportThreadDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(thread);
            apiResponse.Message.Should().Be("Support thread created successfully.");

            _supportServiceMock.Verify(
                s => s.AdminCreateThreadAsync(AdminId, targetUserId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task ClaimThread_ShouldPassThreadIdAndAdminIdToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var threadId = 1;

            var thread = new SupportThreadDto();

            _supportServiceMock
                .Setup(s => s.ClaimThreadAsync(threadId, AdminId))
                .ReturnsAsync(thread);

            // Act
            var response = await controller.ClaimThread(threadId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<SupportThreadDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(thread);
            apiResponse.Message.Should().Be("Thread claimed successfully.");

            _supportServiceMock.Verify(
                s => s.ClaimThreadAsync(threadId, AdminId),
                Times.Once
            );
        }
    }
}