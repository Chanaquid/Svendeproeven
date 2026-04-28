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
    public class ConversationControllerTests
    {
        private readonly Mock<IDirectConversationService> _conversationServiceMock = new();
        private readonly Mock<IDirectMessageService> _messageServiceMock = new();

        private const string UserId = "user-123";

        private ConversationController CreateController()
        {
            var controller = new ConversationController(
                _conversationServiceMock.Object,
                _messageServiceMock.Object
            );

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
        public async Task GetOrCreateConversation_ShouldPassCurrentUserIdAndOtherUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var otherUserId = "user-456";

            var conversation = new DirectConversationDto
            {
                Id = 1
            };

            _conversationServiceMock
                .Setup(s => s.GetOrCreateConversationAsync(UserId, otherUserId, null))
                .ReturnsAsync(conversation);

            // Act
            var response = await controller.GetOrCreateConversation(otherUserId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<DirectConversationDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(conversation);
            apiResponse.Message.Should().Be("Conversation ready");

            _conversationServiceMock.Verify(
                s => s.GetOrCreateConversationAsync(UserId, otherUserId, null),
                Times.Once
            );
        }

        [Fact]
        public async Task GetMyConversations_ShouldPassCurrentUserIdFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController();

            var filter = new ConversationFilter();
            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<DirectConversationListDto>
            {
                Items = new List<DirectConversationListDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _conversationServiceMock
                .Setup(s => s.GetUserConversationsAsync(UserId, filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetMyConversations(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<DirectConversationListDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _conversationServiceMock.Verify(
                s => s.GetUserConversationsAsync(UserId, filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task GetConversation_ShouldPassConversationIdAndCurrentUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var conversationId = 1;

            var conversation = new DirectConversationDto
            {
                Id = conversationId
            };

            _conversationServiceMock
                .Setup(s => s.GetConversationAsync(conversationId, UserId))
                .ReturnsAsync(conversation);

            // Act
            var response = await controller.GetConversation(conversationId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<DirectConversationDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(conversation);

            _conversationServiceMock.Verify(
                s => s.GetConversationAsync(conversationId, UserId),
                Times.Once
            );
        }

        [Fact]
        public async Task DeleteConversation_ShouldPassConversationIdAndCurrentUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var conversationId = 1;

            _conversationServiceMock
                .Setup(s => s.DeleteConversationForUserAsync(conversationId, UserId))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.DeleteConversation(conversationId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Message.Should().Be("Conversation deleted");

            _conversationServiceMock.Verify(
                s => s.DeleteConversationForUserAsync(conversationId, UserId),
                Times.Once
            );
        }

        [Fact]
        public async Task RestoreConversation_ShouldPassConversationIdAndCurrentUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var conversationId = 1;

            _conversationServiceMock
                .Setup(s => s.RestoreConversationForUserAsync(conversationId, UserId))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.RestoreConversation(conversationId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Message.Should().Be("Conversation restored");

            _conversationServiceMock.Verify(
                s => s.RestoreConversationForUserAsync(conversationId, UserId),
                Times.Once
            );
        }

        [Fact]
        public async Task SendMessage_ShouldPassConversationIdCurrentUserIdAndContentToService()
        {
            // Arrange
            var controller = CreateController();

            var conversationId = 1;

            var dto = new SendDirectMessageDto
            {
                Content = "Hello there"
            };

            var message = new DirectMessageDto
            {
                Id = 1,
                Content = dto.Content
            };

            _messageServiceMock
                .Setup(s => s.SendMessageAsync(conversationId, UserId, dto.Content))
                .ReturnsAsync(message);

            // Act
            var response = await controller.SendMessage(conversationId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<DirectMessageDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(message);
            apiResponse.Message.Should().Be("Message sent");

            _messageServiceMock.Verify(
                s => s.SendMessageAsync(conversationId, UserId, dto.Content),
                Times.Once
            );
        }

        [Fact]
        public async Task GetMessages_ShouldPassConversationIdCurrentUserIdFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController();

            var conversationId = 1;
            var filter = new MessageFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<DirectMessageDto>
            {
                Items = new List<DirectMessageDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _messageServiceMock
                .Setup(s => s.GetConversationMessagesAsync(conversationId, UserId, filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetMessages(conversationId, filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<DirectMessageDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _messageServiceMock.Verify(
                s => s.GetConversationMessagesAsync(conversationId, UserId, filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task MarkAsRead_ShouldPassConversationIdAndCurrentUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var conversationId = 1;

            _messageServiceMock
                .Setup(s => s.MarkMessagesAsReadAsync(conversationId, UserId))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.MarkAsRead(conversationId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Message.Should().Be("Messages marked as read");

            _messageServiceMock.Verify(
                s => s.MarkMessagesAsReadAsync(conversationId, UserId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetTotalUnreadCount_ShouldReturnUnreadCount()
        {
            // Arrange
            var controller = CreateController();

            var unreadCount = 5;

            _conversationServiceMock
                .Setup(s => s.GetTotalUnreadCountAsync(UserId))
                .ReturnsAsync(unreadCount);

            // Act
            var response = await controller.GetTotalUnreadCount();

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<int>>()
                .Subject;

            apiResponse.Data.Should().Be(unreadCount);

            _conversationServiceMock.Verify(
                s => s.GetTotalUnreadCountAsync(UserId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetUnreadCountsPerConversation_ShouldReturnUnreadCounts()
        {
            // Arrange
            var controller = CreateController();

            var unreadCounts = new UnreadCountsDto();

            _conversationServiceMock
                .Setup(s => s.GetUnreadCountsPerConversationAsync(UserId))
                .ReturnsAsync(unreadCounts);

            // Act
            var response = await controller.GetUnreadCountsPerConversation();

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<UnreadCountsDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(unreadCounts);

            _conversationServiceMock.Verify(
                s => s.GetUnreadCountsPerConversationAsync(UserId),
                Times.Once
            );
        }
    }
}