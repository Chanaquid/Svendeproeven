using backend.Dtos;
using backend.Hubs;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace backend.Tests.Services
{
    public class DirectMessageServiceTests
    {
        private readonly Mock<IDirectMessageRepository> _messageRepositoryMock = new();
        private readonly Mock<IDirectConversationRepository> _conversationRepositoryMock = new();
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IHubContext<DirectChatHub>> _hubContextMock = new();
        private readonly Mock<IHubContext<NotificationHub>> _notificationHubMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly Mock<IOnlineTracker> _onlineTrackerMock = new();


        public DirectMessageServiceTests()
        {
            _userManagerMock = MockUserManager();
        }

        private DirectMessageService CreateService()
        {
            SetupSignalR();

            return new DirectMessageService(
                _messageRepositoryMock.Object,
                _conversationRepositoryMock.Object,
                _userManagerMock.Object,
                _hubContextMock.Object,
                _notificationHubMock.Object,
                _notificationServiceMock.Object,
                _onlineTrackerMock.Object
            );
        }

        private static Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();

            return new Mock<UserManager<ApplicationUser>>(
                store.Object,
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<IPasswordHasher<ApplicationUser>>(),
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                Mock.Of<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<ApplicationUser>>>()
            );
        }

        private static ApplicationUser CreateUser(string id = "user-1")
        {
            return new ApplicationUser
            {
                Id = id,
                FullName = "Test User",
                UserName = "testuser",
                AvatarUrl = "avatar.png"
            };
        }

        private static DirectConversation CreateConversation()
        {
            return new DirectConversation
            {
                Id = 1,
                InitiatedById = "user-1",
                OtherUserId = "user-2",
                MessageCount = 0,
                CreatedAt = DateTime.UtcNow
            };
        }

        private void SetupSignalR()
        {
            var clientsMock = new Mock<IHubClients>();
            var clientProxyMock = new Mock<IClientProxy>();

            clientProxyMock
                .Setup(x => x.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object?[]>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            clientsMock
                .Setup(c => c.Group(It.IsAny<string>()))
                .Returns(clientProxyMock.Object);

            _hubContextMock
                .Setup(h => h.Clients)
                .Returns(clientsMock.Object);

            _notificationHubMock
                .Setup(h => h.Clients)
                .Returns(clientsMock.Object);
        }
        [Fact]
        public async Task SendMessageAsync_ShouldThrow_WhenMessageIsEmpty()
        {
            // Arrange
            var service = CreateService();

            // Act
            Func<Task> act = async () =>
                await service.SendMessageAsync(1, "user-1", "   ");

            // Assert
            await act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("Message cannot be empty.");
        }

        [Fact]
        public async Task SendMessageAsync_ShouldThrow_WhenMessageIsTooLong()
        {
            // Arrange
            var service = CreateService();

            var longMessage = new string('a', 2001);

            // Act
            Func<Task> act = async () =>
                await service.SendMessageAsync(1, "user-1", longMessage);

            // Assert
            await act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("Message cannot exceed 2000 characters.");
        }

        [Fact]
        public async Task SendMessageAsync_ShouldThrow_WhenConversationDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            _conversationRepositoryMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync((DirectConversation?)null);

            // Act
            Func<Task> act = async () =>
                await service.SendMessageAsync(1, "user-1", "Hello");

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Conversation not found.");
        }

        [Fact]
        public async Task SendMessageAsync_ShouldThrow_WhenSenderIsNotParticipant()
        {
            // Arrange
            var service = CreateService();

            var conversation = CreateConversation();

            _conversationRepositoryMock
                .Setup(r => r.GetByIdAsync(conversation.Id))
                .ReturnsAsync(conversation);

            // Act
            Func<Task> act = async () =>
                await service.SendMessageAsync(conversation.Id, "stranger-user", "Hello");

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You are not a participant in this conversation.");
        }

        [Fact]
        public async Task SendMessageAsync_ShouldThrow_WhenUsersAreBlocked()
        {
            // Arrange
            var service = CreateService();

            var conversation = CreateConversation();

            _conversationRepositoryMock
                .Setup(r => r.GetByIdAsync(conversation.Id))
                .ReturnsAsync(conversation);

            _conversationRepositoryMock
                .Setup(r => r.AreUsersBlockedAsync("user-1", "user-2"))
                .ReturnsAsync(true);

            // Act
            Func<Task> act = async () =>
                await service.SendMessageAsync(conversation.Id, "user-1", "Hello");

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("This conversation is unavailable.");
        }

        [Fact]
        public async Task SendMessageAsync_ShouldCreateMessageUpdateConversationAndReturnDto_WhenValid()
        {
            // Arrange
            var service = CreateService();

            SetupSignalR();

            var conversation = CreateConversation();
            var sender = CreateUser("user-1");

            _conversationRepositoryMock
                .Setup(r => r.GetByIdAsync(conversation.Id))
                .ReturnsAsync(conversation);

            _conversationRepositoryMock
                .Setup(r => r.AreUsersBlockedAsync("user-1", "user-2"))
                .ReturnsAsync(false);

            _messageRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<DirectMessage>()))
                .Callback<DirectMessage>(message =>
                {
                    message.Id = 10;
                })
                .Returns(Task.CompletedTask);

            _messageRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _conversationRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _userManagerMock
                .Setup(m => m.FindByIdAsync("user-1"))
                .ReturnsAsync(sender);

            // Act
            var result = await service.SendMessageAsync(conversation.Id, "user-1", " Hello ");

            // Assert
            result.Id.Should().Be(10);
            result.ConversationId.Should().Be(conversation.Id);
            result.SenderId.Should().Be("user-1");
            result.Content.Should().Be("Hello");
            result.IsMine.Should().BeTrue();
            result.SenderFullName.Should().Be(sender.FullName);

            conversation.MessageCount.Should().Be(1);
            conversation.LastMessageId.Should().Be(10);
            conversation.LastMessageAt.Should().NotBeNull();

            _messageRepositoryMock.Verify(r => r.AddAsync(It.Is<DirectMessage>(m =>
                m.ConversationId == conversation.Id &&
                m.SenderId == "user-1" &&
                m.Content == "Hello" &&
                m.IsRead == false
            )), Times.Once);

            _messageRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
            _conversationRepositoryMock.Verify(r => r.Update(conversation), Times.Once);
            _conversationRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetConversationMessagesAsync_ShouldReturnMessages_WhenUserHasAccess()
        {
            // Arrange
            var service = CreateService();

            var conversation = CreateConversation();
            var sender = CreateUser("user-1");

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            _conversationRepositoryMock
                .Setup(r => r.GetByIdAsync(conversation.Id))
                .ReturnsAsync(conversation);

            _messageRepositoryMock
                .Setup(r => r.GetConversationMessagesAsync(
                    conversation.Id,
                    "user-1",
                    null,
                    request))
                .ReturnsAsync(new PagedResult<DirectMessage>
                {
                    Items = new List<DirectMessage>
                    {
                        new DirectMessage
                        {
                            Id = 10,
                            ConversationId = conversation.Id,
                            SenderId = "user-1",
                            Sender = sender,
                            Content = "Hello",
                            IsRead = false,
                            SentAt = DateTime.UtcNow
                        }
                    },
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 10
                });

            // Act
            var result = await service.GetConversationMessagesAsync(
                conversation.Id,
                "user-1",
                null,
                request);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items[0].Content.Should().Be("Hello");
            result.Items[0].IsMine.Should().BeTrue();
            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetConversationMessagesAsync_ShouldThrow_WhenUserHasNoAccess()
        {
            // Arrange
            var service = CreateService();

            var conversation = CreateConversation();

            _conversationRepositoryMock
                .Setup(r => r.GetByIdAsync(conversation.Id))
                .ReturnsAsync(conversation);

            // Act
            Func<Task> act = async () =>
                await service.GetConversationMessagesAsync(
                    conversation.Id,
                    "stranger-user",
                    null,
                    new PagedRequest());

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You don't have access to this conversation.");
        }

        [Fact]
        public async Task GetUnreadCountAsync_ShouldReturnRepositoryCount()
        {
            // Arrange
            var service = CreateService();

            _messageRepositoryMock
                .Setup(r => r.GetTotalUnreadCountForUserAsync("user-1"))
                .ReturnsAsync(5);

            // Act
            var result = await service.GetUnreadCountAsync("user-1");

            // Assert
            result.Should().Be(5);
        }

        [Fact]
        public async Task GetLastMessageAsync_ShouldReturnNull_WhenNoMessageExists()
        {
            // Arrange
            var service = CreateService();

            _messageRepositoryMock
                .Setup(r => r.GetLastMessageAsync(1))
                .ReturnsAsync((DirectMessage?)null);

            // Act
            var result = await service.GetLastMessageAsync(1, "user-1");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetLastMessageAsync_ShouldReturnMappedMessage_WhenMessageExists()
        {
            // Arrange
            var service = CreateService();

            var sender = CreateUser("user-1");

            var message = new DirectMessage
            {
                Id = 10,
                ConversationId = 1,
                SenderId = "user-1",
                Sender = sender,
                Content = "Last message",
                SentAt = DateTime.UtcNow,
                IsRead = true
            };

            _messageRepositoryMock
                .Setup(r => r.GetLastMessageAsync(1))
                .ReturnsAsync(message);

            // Act
            var result = await service.GetLastMessageAsync(1, "user-1");

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(message.Id);
            result.Content.Should().Be("Last message");
            result.IsMine.Should().BeTrue();
            result.SenderUserName.Should().Be(sender.UserName);
        }
    }
}