using backend.Common;
using backend.Controllers;
using backend.Dtos;
using backend.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using System.Security.Claims;
using Xunit;

namespace backend.Tests.Controllers
{
    public class LoanMessageControllerTests
    {
        private readonly Mock<ILoanMessageService> _loanMessageServiceMock = new();
        private readonly Mock<IHubContext<LoanChatHub>> _hubContextMock = new();
        private readonly Mock<IHubClients> _hubClientsMock = new();
        private readonly Mock<IClientProxy> _clientProxyMock = new();

        private const string UserId = "user-123";
        private const string AdminId = "admin-123";

        private LoanMessageController CreateController(bool isAdmin = false)
        {
            _hubContextMock
        .Setup(h => h.Clients)
        .Returns(_hubClientsMock.Object);

            _hubClientsMock
                .Setup(c => c.Group(It.IsAny<string>()))
                .Returns(_clientProxyMock.Object);

            _clientProxyMock
                .Setup(c => c.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);


            var controller = new LoanMessageController(_loanMessageServiceMock.Object,
                    _hubContextMock.Object
);

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
        public async Task GetMessages_ShouldPassLoanIdUserIdIsAdminAndRequestToService_WhenUser()
        {
            // Arrange
            var controller = CreateController();

            var loanId = 1;

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<LoanMessageDto>
            {
                Items = new List<LoanMessageDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _loanMessageServiceMock
                .Setup(s => s.GetMessagesAsync(loanId, UserId, false, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetMessages(loanId, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<LoanMessageDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _loanMessageServiceMock.Verify(
                s => s.GetMessagesAsync(loanId, UserId, false, request),
                Times.Once
            );
        }

        [Fact]
        public async Task GetMessages_ShouldPassIsAdminTrue_WhenAdmin()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var loanId = 1;

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<LoanMessageDto>
            {
                Items = new List<LoanMessageDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _loanMessageServiceMock
                .Setup(s => s.GetMessagesAsync(loanId, AdminId, true, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetMessages(loanId, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<LoanMessageDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _loanMessageServiceMock.Verify(
                s => s.GetMessagesAsync(loanId, AdminId, true, request),
                Times.Once
            );
        }

        [Fact]
        public async Task SendMessage_ShouldPassLoanIdUserIdDtoAndIsAdminFalseToService_WhenUser()
        {
            // Arrange
            var controller = CreateController();

            var loanId = 1;

            var dto = new SendLoanMessageDto
            {
                Content = "Hello"
            };

            var message = new LoanMessageDto();

            _loanMessageServiceMock
                .Setup(s => s.SendMessageAsync(loanId, UserId, dto, false))
                .ReturnsAsync(message);

            // Act
            var response = await controller.SendMessage(loanId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<LoanMessageDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(message);

            _loanMessageServiceMock.Verify(
                s => s.SendMessageAsync(loanId, UserId, dto, false),
                Times.Once
            );
        }

        [Fact]
        public async Task SendMessage_ShouldPassIsAdminTrue_WhenAdmin()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var loanId = 1;

            var dto = new SendLoanMessageDto
            {
                Content = "Admin message"
            };

            var message = new LoanMessageDto();

            _loanMessageServiceMock
                .Setup(s => s.SendMessageAsync(loanId, AdminId, dto, true))
                .ReturnsAsync(message);

            // Act
            var response = await controller.SendMessage(loanId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<LoanMessageDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(message);

            _loanMessageServiceMock.Verify(
                s => s.SendMessageAsync(loanId, AdminId, dto, true),
                Times.Once
            );
        }

        [Fact]
        public async Task MarkAsRead_ShouldPassLoanIdUserIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController();

            var loanId = 1;

            var dto = new MarkLoanMessagesReadDto();

            _loanMessageServiceMock
                .Setup(s => s.MarkAsReadAsync(loanId, UserId, dto))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.MarkAsRead(loanId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Success.Should().BeTrue();

            _loanMessageServiceMock.Verify(
                s => s.MarkAsReadAsync(loanId, UserId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task GetUnreadCount_ShouldReturnLoanIdAndUnreadCount()
        {
            // Arrange
            var controller = CreateController();

            var loanId = 1;
            var unreadCount = 5;

            _loanMessageServiceMock
                .Setup(s => s.GetUnreadCountAsync(loanId, UserId))
                .ReturnsAsync(unreadCount);

            // Act
            var response = await controller.GetUnreadCount(loanId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<LoanUnreadCountDto>>()
                .Subject;

            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data!.LoanId.Should().Be(loanId);
            apiResponse.Data.UnreadCount.Should().Be(unreadCount);

            _loanMessageServiceMock.Verify(
                s => s.GetUnreadCountAsync(loanId, UserId),
                Times.Once
            );
        }
    }
}