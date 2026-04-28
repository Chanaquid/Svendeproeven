using backend.Dtos;
using backend.Hubs;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace backend.Tests.Services
{
    public class SupportServiceTests
    {
        private readonly Mock<ISupportRepository> _supportRepositoryMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock = new(
            Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
        private readonly Mock<IHubContext<SupportChatHub>> _hubContextMock = new();
        private readonly Mock<IHubClients> _hubClientsMock = new();
        private readonly Mock<IClientProxy> _clientProxyMock = new();

        private SupportService CreateService()
        {
            _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);
            _hubClientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
            _clientProxyMock
                .Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            return new SupportService(
                _supportRepositoryMock.Object,
                _userRepositoryMock.Object,
                _userManagerMock.Object,
                _notificationServiceMock.Object,
                _hubContextMock.Object
            );
        }

        private static ApplicationUser CreateUser(string id = "user-123")
        {
            return new ApplicationUser
            {
                Id = id,
                FullName = "Test User",
                UserName = "testuser",
                AvatarUrl = "avatar.png"
            };
        }

        private static ApplicationUser CreateAdmin(string id = "admin-123")
        {
            return new ApplicationUser
            {
                Id = id,
                FullName = "Admin User",
                UserName = "adminuser",
                AvatarUrl = "admin.png"
            };
        }

        private static SupportThread CreateThread(
            int id = 1,
            string userId = "user-123",
            SupportThreadStatus status = SupportThreadStatus.Open)
        {
            var user = CreateUser(userId);

            return new SupportThread
            {
                Id = id,
                UserId = userId,
                User = user,
                Subject = "Test support thread",
                Status = status,
                CreatedAt = DateTime.UtcNow,
                Messages = new List<SupportMessage>()
            };
        }

        private static SupportMessage CreateMessage(
            int id = 1,
            int threadId = 1,
            string senderId = "user-123",
            string content = "Hello")
        {
            var sender = CreateUser(senderId);

            return new SupportMessage
            {
                Id = id,
                SupportThreadId = threadId,
                SenderId = senderId,
                Sender = sender,
                Content = content,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };
        }

        [Fact]
        public async Task CreateThreadAsync_ShouldThrow_WhenUserDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var dto = new CreateSupportThreadDto
            {
                Subject = "Help",
                InitialMessage = "I need help"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync("missing-user"))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            Func<Task> act = async () => await service.CreateThreadAsync("missing-user", dto);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("User not found.");

            _supportRepositoryMock.Verify(r => r.AddThreadAsync(It.IsAny<SupportThread>()), Times.Never);
        }

        [Fact]
        public async Task CreateThreadAsync_ShouldThrow_WhenUserAlreadyHasActiveThread()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser();

            var dto = new CreateSupportThreadDto
            {
                Subject = "Help",
                InitialMessage = "I need help"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _supportRepositoryMock
                .Setup(r => r.HasActiveThreadAsync(user.Id))
                .ReturnsAsync(true);

            // Act
            Func<Task> act = async () => await service.CreateThreadAsync(user.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("You already have an open support thread. Please wait for it to be resolved before opening a new one.");

            _supportRepositoryMock.Verify(r => r.AddThreadAsync(It.IsAny<SupportThread>()), Times.Never);
        }

        [Fact]
        public async Task CreateThreadAsync_ShouldCreateThreadAndInitialMessage_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser();

            var dto = new CreateSupportThreadDto
            {
                Subject = " Login problem ",
                InitialMessage = " I cannot log in "
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _supportRepositoryMock
                .Setup(r => r.HasActiveThreadAsync(user.Id))
                .ReturnsAsync(false);

            _supportRepositoryMock
                .Setup(r => r.AddThreadAsync(It.IsAny<SupportThread>()))
                .Callback<SupportThread>(thread =>
                {
                    thread.Id = 1;
                    thread.User = user;
                    thread.Messages = new List<SupportMessage>();
                })
                .Returns(Task.CompletedTask);

            _supportRepositoryMock
                .Setup(r => r.AddMessageAsync(It.IsAny<SupportMessage>()))
                .Callback<SupportMessage>(message =>
                {
                    message.Id = 1;
                    message.Sender = user;
                })
                .Returns(Task.CompletedTask);

            _supportRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _notificationServiceMock
                .Setup(n => n.SendAsync(
                    It.IsAny<string>(),
                    It.IsAny<NotificationType>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<NotificationReferenceType?>()))
                .Returns(Task.CompletedTask);

            var createdThread = CreateThread(1, user.Id);
            createdThread.Subject = "Login problem";
            createdThread.Messages.Add(CreateMessage(1, 1, user.Id, "I cannot log in"));

            _supportRepositoryMock
                .Setup(r => r.GetThreadByIdWithMessagesAsync(1))
                .ReturnsAsync(createdThread);

            // Act
            var result = await service.CreateThreadAsync(user.Id, dto);

            // Assert
            result.Id.Should().Be(1);
            result.Subject.Should().Be("Login problem");
            result.UserId.Should().Be(user.Id);
            result.Messages.Should().HaveCount(1);
            result.Messages[0].Content.Should().Be("I cannot log in");

            _supportRepositoryMock.Verify(r => r.AddThreadAsync(It.Is<SupportThread>(t =>
                t.UserId == user.Id &&
                t.Subject == "Login problem" &&
                t.Status == SupportThreadStatus.Open
            )), Times.Once);

            _supportRepositoryMock.Verify(r => r.AddMessageAsync(It.Is<SupportMessage>(m =>
                m.SupportThreadId == 1 &&
                m.SenderId == user.Id &&
                m.Content == "I cannot log in"
            )), Times.Once);

            _supportRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task GetThreadByIdAsync_ShouldThrow_WhenThreadDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            _supportRepositoryMock
                .Setup(r => r.GetThreadByIdWithMessagesAsync(99))
                .ReturnsAsync((SupportThread?)null);

            // Act
            Func<Task> act = async () => await service.GetThreadByIdAsync(99, "user-123", isAdmin: false);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Support thread not found.");
        }

        [Fact]
        public async Task GetThreadByIdAsync_ShouldThrow_WhenUserDoesNotOwnThread()
        {
            // Arrange
            var service = CreateService();

            var thread = CreateThread(userId: "owner-user");

            _supportRepositoryMock
                .Setup(r => r.GetThreadByIdWithMessagesAsync(thread.Id))
                .ReturnsAsync(thread);

            // Act
            Func<Task> act = async () => await service.GetThreadByIdAsync(thread.Id, "another-user", isAdmin: false);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You do not have access to this thread.");
        }

        [Fact]
        public async Task GetThreadByIdAsync_ShouldReturnThread_WhenUserOwnsThread()
        {
            // Arrange
            var service = CreateService();

            var thread = CreateThread();
            thread.Messages.Add(CreateMessage());

            _supportRepositoryMock
                .Setup(r => r.GetThreadByIdWithMessagesAsync(thread.Id))
                .ReturnsAsync(thread);

            // Act
            var result = await service.GetThreadByIdAsync(thread.Id, thread.UserId, isAdmin: false);

            // Assert
            result.Id.Should().Be(thread.Id);
            result.UserId.Should().Be(thread.UserId);
            result.Messages.Should().HaveCount(1);
        }

        [Fact]
        public async Task SendMessageAsync_ShouldThrow_WhenThreadDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var dto = new SendSupportMessageDto
            {
                Content = "Hello"
            };

            _supportRepositoryMock
                .Setup(r => r.GetThreadByIdAsync(1))
                .ReturnsAsync((SupportThread?)null);

            // Act
            Func<Task> act = async () => await service.SendMessageAsync(1, "user-123", dto, isAdmin: false);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Support thread not found.");
        }

        [Fact]
        public async Task SendMessageAsync_ShouldThrow_WhenUserHasNoAccess()
        {
            // Arrange
            var service = CreateService();

            var thread = CreateThread(userId: "owner-user");

            var dto = new SendSupportMessageDto
            {
                Content = "Hello"
            };

            _supportRepositoryMock
                .Setup(r => r.GetThreadByIdAsync(thread.Id))
                .ReturnsAsync(thread);

            // Act
            Func<Task> act = async () => await service.SendMessageAsync(thread.Id, "another-user", dto, isAdmin: false);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You do not have access to this thread.");

            _supportRepositoryMock.Verify(r => r.AddMessageAsync(It.IsAny<SupportMessage>()), Times.Never);
        }

        [Fact]
        public async Task SendMessageAsync_ShouldThrow_WhenThreadIsClosed()
        {
            // Arrange
            var service = CreateService();

            var thread = CreateThread(status: SupportThreadStatus.Closed);

            var dto = new SendSupportMessageDto
            {
                Content = "Hello"
            };

            _supportRepositoryMock
                .Setup(r => r.GetThreadByIdAsync(thread.Id))
                .ReturnsAsync(thread);

            // Act
            Func<Task> act = async () => await service.SendMessageAsync(thread.Id, thread.UserId, dto, isAdmin: false);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("This support thread is closed. No further messages can be sent.");
        }

        [Fact]
        public async Task SendMessageAsync_ShouldAddMessage_WhenUserOwnsThread()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser();
            var thread = CreateThread(userId: user.Id);

            var dto = new SendSupportMessageDto
            {
                Content = " Hello support "
            };

            _supportRepositoryMock
                .Setup(r => r.GetThreadByIdAsync(thread.Id))
                .ReturnsAsync(thread);

            _supportRepositoryMock
                .Setup(r => r.AddMessageAsync(It.IsAny<SupportMessage>()))
                .Callback<SupportMessage>(message =>
                {
                    message.Id = 10;
                })
                .Returns(Task.CompletedTask);

            _supportRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            var result = await service.SendMessageAsync(thread.Id, user.Id, dto, isAdmin: false);

            // Assert
            result.Id.Should().Be(10);
            result.SupportThreadId.Should().Be(thread.Id);
            result.SenderId.Should().Be(user.Id);
            result.Content.Should().Be("Hello support");
            result.IsMine.Should().BeTrue();

            _supportRepositoryMock.Verify(r => r.AddMessageAsync(It.Is<SupportMessage>(m =>
                m.SupportThreadId == thread.Id &&
                m.SenderId == user.Id &&
                m.Content == "Hello support"
            )), Times.Once);

            _supportRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CloseThreadAsync_ShouldCloseThread_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var thread = CreateThread();

            _supportRepositoryMock
                .Setup(r => r.GetThreadByIdAsync(thread.Id))
                .ReturnsAsync(thread);

            _supportRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.CloseThreadAsync(thread.Id, thread.UserId, isAdmin: false);

            // Assert
            thread.Status.Should().Be(SupportThreadStatus.Closed);
            thread.ClosedAt.Should().NotBeNull();

            _supportRepositoryMock.Verify(r => r.UpdateThread(thread), Times.Once);
            _supportRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CloseThreadAsync_ShouldThrow_WhenAlreadyClosed()
        {
            // Arrange
            var service = CreateService();

            var thread = CreateThread(status: SupportThreadStatus.Closed);

            _supportRepositoryMock
                .Setup(r => r.GetThreadByIdAsync(thread.Id))
                .ReturnsAsync(thread);

            // Act
            Func<Task> act = async () => await service.CloseThreadAsync(thread.Id, thread.UserId, isAdmin: false);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("This thread is already closed.");
        }

        [Fact]
        public async Task AdminCreateThreadAsync_ShouldThrow_WhenTargetUserDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var dto = new CreateSupportThreadDto
            {
                Subject = "Admin thread",
                InitialMessage = "Hello from admin"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync("missing-user"))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            Func<Task> act = async () => await service.AdminCreateThreadAsync("admin-123", "missing-user", dto);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Target user not found.");
        }

        [Fact]
        public async Task AdminCreateThreadAsync_ShouldCreateClaimedThread()
        {
            // Arrange
            var service = CreateService();

            var targetUser = CreateUser("target-user");
            var admin = CreateAdmin();

            var dto = new CreateSupportThreadDto
            {
                Subject = " Admin thread ",
                InitialMessage = " Hello from admin "
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(targetUser.Id))
                .ReturnsAsync(targetUser);

            _supportRepositoryMock
                .Setup(r => r.AddThreadAsync(It.IsAny<SupportThread>()))
                .Callback<SupportThread>(thread =>
                {
                    thread.Id = 1;
                    thread.User = targetUser;
                    thread.ClaimedByAdmin = admin;
                    thread.Messages = new List<SupportMessage>();
                })
                .Returns(Task.CompletedTask);

            _supportRepositoryMock
                .Setup(r => r.AddMessageAsync(It.IsAny<SupportMessage>()))
                .Callback<SupportMessage>(message =>
                {
                    message.Id = 1;
                    message.Sender = admin;
                })
                .Returns(Task.CompletedTask);

            _supportRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _notificationServiceMock
                .Setup(n => n.SendAsync(
                    It.IsAny<string>(),
                    It.IsAny<NotificationType>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<NotificationReferenceType?>()))
                .Returns(Task.CompletedTask);

            var created = CreateThread(1, targetUser.Id, SupportThreadStatus.Claimed);
            created.Subject = "Admin thread";
            created.ClaimedByAdminId = admin.Id;
            created.ClaimedByAdmin = admin;
            created.Messages.Add(CreateMessage(1, 1, admin.Id, "Hello from admin"));

            _supportRepositoryMock
                .Setup(r => r.GetThreadByIdWithMessagesAsync(1))
                .ReturnsAsync(created);

            // Act
            var result = await service.AdminCreateThreadAsync(admin.Id, targetUser.Id, dto);

            // Assert
            result.Id.Should().Be(1);
            result.UserId.Should().Be(targetUser.Id);
            result.Status.Should().Be(SupportThreadStatus.Claimed);
            result.ClaimedByAdminId.Should().Be(admin.Id);
            result.Messages.Should().HaveCount(1);

            _supportRepositoryMock.Verify(r => r.AddThreadAsync(It.Is<SupportThread>(t =>
                t.UserId == targetUser.Id &&
                t.Subject == "Admin thread" &&
                t.Status == SupportThreadStatus.Claimed &&
                t.ClaimedByAdminId == admin.Id
            )), Times.Once);
        }

        [Fact]
        public async Task ClaimThreadAsync_ShouldThrow_WhenThreadIsClosed()
        {
            // Arrange
            var service = CreateService();

            var thread = CreateThread(status: SupportThreadStatus.Closed);

            _supportRepositoryMock
                .Setup(r => r.GetThreadByIdWithMessagesAsync(thread.Id))
                .ReturnsAsync(thread);

            // Act
            Func<Task> act = async () => await service.ClaimThreadAsync(thread.Id, "admin-123");

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Cannot claim a closed thread.");
        }

        [Fact]
        public async Task ClaimThreadAsync_ShouldClaimThread_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var thread = CreateThread(status: SupportThreadStatus.Open);
            var admin = CreateAdmin();

            _supportRepositoryMock
                .Setup(r => r.GetThreadByIdWithMessagesAsync(thread.Id))
                .ReturnsAsync(thread);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(admin.Id))
                .ReturnsAsync(admin);

            _supportRepositoryMock
                .Setup(r => r.AddMessageAsync(It.IsAny<SupportMessage>()))
                .Returns(Task.CompletedTask);

            _supportRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _notificationServiceMock
                .Setup(n => n.SendAsync(
                    It.IsAny<string>(),
                    It.IsAny<NotificationType>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<NotificationReferenceType?>()))
                .Returns(Task.CompletedTask);

            _supportRepositoryMock
                .Setup(r => r.GetThreadByIdWithMessagesAsync(thread.Id))
                .ReturnsAsync(thread);

            // Act
            var result = await service.ClaimThreadAsync(thread.Id, admin.Id);

            // Assert
            thread.ClaimedByAdminId.Should().Be(admin.Id);
            thread.Status.Should().Be(SupportThreadStatus.Claimed);

            result.Status.Should().Be(SupportThreadStatus.Claimed);

            _supportRepositoryMock.Verify(r => r.UpdateThread(thread), Times.Once);
            _supportRepositoryMock.Verify(r => r.AddMessageAsync(It.Is<SupportMessage>(m =>
                m.SupportThreadId == thread.Id &&
                m.SenderId == admin.Id &&
                m.Content.Contains("I've picked up your thread")
            )), Times.Once);
            _supportRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetMyThreadsAsync_ShouldReturnPagedThreads()
        {
            // Arrange
            var service = CreateService();

            var userId = "user-123";

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var thread = CreateThread(userId: userId);
            var lastMessage = CreateMessage(threadId: thread.Id, senderId: userId, content: "Last message");

            _supportRepositoryMock
                .Setup(r => r.GetThreadsByUserIdAsync(userId, null, request))
                .ReturnsAsync(new PagedResult<SupportThread>
                {
                    Items = new List<SupportThread> { thread },
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 10
                });

            _supportRepositoryMock
                .Setup(r => r.GetLastMessageAsync(thread.Id))
                .ReturnsAsync(lastMessage);

            _supportRepositoryMock
                .Setup(r => r.GetUnreadCountAsync(thread.Id, userId))
                .ReturnsAsync(2);

            // Act
            var result = await service.GetMyThreadsAsync(userId, null, request);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items[0].Id.Should().Be(thread.Id);
            result.Items[0].LastMessagePreview.Should().Be("Last message");
            result.Items[0].UnreadCount.Should().Be(2);
            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task AutoCloseInactiveThreadsAsync_ShouldCloseInactiveThreads()
        {
            // Arrange
            var service = CreateService();

            var thread1 = CreateThread(1, "user-1", SupportThreadStatus.Open);
            var thread2 = CreateThread(2, "user-2", SupportThreadStatus.Claimed);

            _supportRepositoryMock
                .Setup(r => r.GetInactiveOpenThreadsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<SupportThread> { thread1, thread2 });

            _notificationServiceMock
                .Setup(n => n.SendAsync(
                    It.IsAny<string>(),
                    It.IsAny<NotificationType>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<NotificationReferenceType?>()))
                .Returns(Task.CompletedTask);

            _supportRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.AutoCloseInactiveThreadsAsync();

            // Assert
            thread1.Status.Should().Be(SupportThreadStatus.Closed);
            thread2.Status.Should().Be(SupportThreadStatus.Closed);

            thread1.ClosedAt.Should().NotBeNull();
            thread2.ClosedAt.Should().NotBeNull();

            _supportRepositoryMock.Verify(r => r.UpdateThread(thread1), Times.Once);
            _supportRepositoryMock.Verify(r => r.UpdateThread(thread2), Times.Once);
            _supportRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}