using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace backend.Tests.Services
{
    public class DirectConversationServiceTests
    {
        private readonly Mock<IDirectConversationRepository> _conversationRepositoryMock = new();
        private readonly Mock<IDirectMessageRepository> _messageRepositoryMock = new();
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

        public DirectConversationServiceTests()
        {
            _userManagerMock = MockUserManager();
        }

        private DirectConversationService CreateService()
        {
            return new DirectConversationService(
                _conversationRepositoryMock.Object,
                _messageRepositoryMock.Object,
                _userManagerMock.Object
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

        private static ApplicationUser CreateUser(string id, string username)
        {
            return new ApplicationUser
            {
                Id = id,
                UserName = username,
                FullName = $"{username} FullName",
                AvatarUrl = $"{username}.png"
            };
        }

        private static DirectConversation CreateConversation()
        {
            var user1 = CreateUser("user-1", "userone");
            var user2 = CreateUser("user-2", "usertwo");

            return new DirectConversation
            {
                Id = 1,
                InitiatedById = user1.Id,
                InitiatedBy = user1,
                OtherUserId = user2.Id,
                OtherUser = user2,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                LastMessageAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task GetOrCreateConversationAsync_ShouldThrow_WhenUserTriesToMessageThemself()
        {
            // Arrange
            var service = CreateService();

            // Act
            Func<Task> act = async () =>
                await service.GetOrCreateConversationAsync("user-1", "user-1");

            // Assert
            await act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("Cannot create a conversation with yourself.");
        }

        [Fact]
        public async Task GetOrCreateConversationAsync_ShouldThrow_WhenUsersAreBlocked()
        {
            // Arrange
            var service = CreateService();

            _conversationRepositoryMock
                .Setup(r => r.AreUsersBlockedAsync("user-1", "user-2"))
                .ReturnsAsync(true);

            // Act
            Func<Task> act = async () =>
                await service.GetOrCreateConversationAsync("user-1", "user-2");

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("This conversation is unavailable.");
        }

        [Fact]
        public async Task GetConversationAsync_ShouldReturnConversation_WhenUserIsParticipant()
        {
            // Arrange
            var service = CreateService();

            var conversation = CreateConversation();

            _conversationRepositoryMock
                .Setup(r => r.GetByIdAsync(conversation.Id))
                .ReturnsAsync(conversation);

            _conversationRepositoryMock
                .Setup(r => r.AreUsersBlockedAsync("user-1", "user-2"))
                .ReturnsAsync(false);

            _messageRepositoryMock
                .Setup(r => r.GetUnreadCountForConversationAsync(
                    conversation.Id,
                    "user-1",
                    conversation.InitiatorDeletedAt))
                .ReturnsAsync(2);

            _messageRepositoryMock
                .Setup(r => r.GetConversationMessagesAsync(
                    conversation.Id,
                    "user-1",
                    null,
                    It.IsAny<PagedRequest>()))
                .ReturnsAsync(new PagedResult<DirectMessage>
                {
                    Items = new List<DirectMessage>
                    {
                        new DirectMessage
                        {
                            Id = 10,
                            ConversationId = conversation.Id,
                            SenderId = "user-1",
                            Sender = conversation.InitiatedBy,
                            Content = "Hello",
                            SentAt = DateTime.UtcNow,
                            IsRead = false
                        }
                    },
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 50
                });

            // Act
            var result = await service.GetConversationAsync(conversation.Id, "user-1");

            // Assert
            result.Id.Should().Be(conversation.Id);
            result.OtherUserId.Should().Be("user-2");
            result.OtherUserName.Should().Be("usertwo");
            result.CanSendMessage.Should().BeTrue();
            result.IsBlocked.Should().BeFalse();
            result.UnreadCount.Should().Be(2);
            result.Messages.Should().HaveCount(1);
            result.Messages[0].IsMine.Should().BeTrue();
        }

        [Fact]
        public async Task GetConversationAsync_ShouldThrow_WhenConversationDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            _conversationRepositoryMock
                .Setup(r => r.GetByIdAsync(99))
                .ReturnsAsync((DirectConversation?)null);

            // Act
            Func<Task> act = async () =>
                await service.GetConversationAsync(99, "user-1");

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Conversation not found.");
        }

        [Fact]
        public async Task GetConversationAsync_ShouldThrow_WhenUserIsNotParticipant()
        {
            // Arrange
            var service = CreateService();

            var conversation = CreateConversation();

            _conversationRepositoryMock
                .Setup(r => r.GetByIdAsync(conversation.Id))
                .ReturnsAsync(conversation);

            // Act
            Func<Task> act = async () =>
                await service.GetConversationAsync(conversation.Id, "stranger-user");

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You don't have access to this conversation.");
        }

        [Fact]
        public async Task DeleteConversationForUserAsync_ShouldHideConversationForInitiator()
        {
            // Arrange
            var service = CreateService();

            var conversation = CreateConversation();

            _conversationRepositoryMock
                .Setup(r => r.GetByIdAsync(conversation.Id))
                .ReturnsAsync(conversation);

            _conversationRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.DeleteConversationForUserAsync(conversation.Id, "user-1");

            // Assert
            conversation.HiddenForInitiator.Should().BeTrue();
            conversation.InitiatorDeletedAt.Should().NotBeNull();

            _conversationRepositoryMock.Verify(r => r.Update(conversation), Times.Once);
            _conversationRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RestoreConversationForUserAsync_ShouldRestoreConversationForInitiator()
        {
            // Arrange
            var service = CreateService();

            var conversation = CreateConversation();
            conversation.HiddenForInitiator = true;
            conversation.InitiatorDeletedAt = DateTime.UtcNow.AddMinutes(-10);

            _conversationRepositoryMock
                .Setup(r => r.GetByIdAsync(conversation.Id))
                .ReturnsAsync(conversation);

            _conversationRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.RestoreConversationForUserAsync(conversation.Id, "user-1");

            // Assert
            conversation.HiddenForInitiator.Should().BeFalse();
            conversation.InitiatorDeletedAt.Should().BeNull();

            _conversationRepositoryMock.Verify(r => r.Update(conversation), Times.Once);
            _conversationRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CanUserAccessConversationAsync_ShouldReturnTrue_WhenUserIsParticipant()
        {
            // Arrange
            var service = CreateService();

            var conversation = CreateConversation();

            _conversationRepositoryMock
                .Setup(r => r.GetByIdAsync(conversation.Id))
                .ReturnsAsync(conversation);

            // Act
            var result = await service.CanUserAccessConversationAsync(conversation.Id, "user-1");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CanUserAccessConversationAsync_ShouldReturnFalse_WhenUserIsNotParticipant()
        {
            // Arrange
            var service = CreateService();

            var conversation = CreateConversation();

            _conversationRepositoryMock
                .Setup(r => r.GetByIdAsync(conversation.Id))
                .ReturnsAsync(conversation);

            // Act
            var result = await service.CanUserAccessConversationAsync(conversation.Id, "stranger-user");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetTotalUnreadCountAsync_ShouldReturnCountFromRepository()
        {
            // Arrange
            var service = CreateService();

            _messageRepositoryMock
                .Setup(r => r.GetTotalUnreadCountForUserAsync("user-1"))
                .ReturnsAsync(5);

            // Act
            var result = await service.GetTotalUnreadCountAsync("user-1");

            // Assert
            result.Should().Be(5);
        }
    }
}