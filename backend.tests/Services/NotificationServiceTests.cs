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
    public class NotificationServiceTests
    {
        private readonly Mock<INotificationRepository> _notificationRepositoryMock = new();
        private readonly Mock<IHubContext<NotificationHub>> _hubContextMock = new();
        private readonly Mock<IHubClients> _hubClientsMock = new();
        private readonly Mock<IClientProxy> _clientProxyMock = new();
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

        public NotificationServiceTests()
        {
            _userManagerMock = MockUserManager();

            _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);
            _hubClientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
            _clientProxyMock
                .Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        private NotificationService CreateService()
        {
            return new NotificationService(
                _notificationRepositoryMock.Object,
                _hubContextMock.Object,
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

        private static Notification CreateNotification(
            int id,
            string userId,
            bool isRead = false)
        {
            return new Notification
            {
                Id = id,
                UserId = userId,
                Type = NotificationType.LoanRequested,
                Message = $"Notification {id}",
                ReferenceId = 10,
                ReferenceType = NotificationReferenceType.Loan,
                IsRead = isRead,
                CreatedAt = DateTime.UtcNow
            };
        }

        private static ApplicationUser CreateAdmin(string id)
        {
            return new ApplicationUser
            {
                Id = id,
                FullName = $"Admin {id}",
                UserName = $"admin{id}"
            };
        }

        [Fact]
        public async Task GetSummaryAsync_ShouldReturnUnreadCountAndLast10Notifications()
        {
            // Arrange
            var service = CreateService();

            var userId = "user-123";

            var notifications = Enumerable.Range(1, 12)
                .Select(i => CreateNotification(i, userId, isRead: i % 2 == 0))
                .ToList();

            _notificationRepositoryMock
                .Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(notifications);

            // Act
            var result = await service.GetSummaryAsync(userId);

            // Assert
            result.UnreadCount.Should().Be(6);
            result.Recent.Should().HaveCount(10);
            result.Recent[0].Id.Should().Be(1);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllNotificationsForUser()
        {
            // Arrange
            var service = CreateService();

            var userId = "user-123";
            var filter = new NotificationFilter();
            var request = new PagedRequest { Page = 1, PageSize = 10 };

            var pagedResult = new PagedResult<NotificationDto>
            {
                Items = new List<NotificationDto>
        {
            new NotificationDto { Id = 1, Message = "Notification 1", IsRead = false },
            new NotificationDto { Id = 2, Message = "Notification 2", IsRead = true }
        },
                TotalCount = 2,
                Page = 1,
                PageSize = 10
            };

            _notificationRepositoryMock
                .Setup(r => r.GetPagedAsync(userId, filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await service.GetAllAsync(userId, filter, request);

            // Assert
            result.Items.Should().HaveCount(2);
            result.Items[0].Id.Should().Be(1);
            result.Items[0].Message.Should().Be("Notification 1");
            result.Items[1].IsRead.Should().BeTrue();
            result.TotalCount.Should().Be(2);
        }


        [Fact]
        public async Task MarkAsReadAsync_ShouldThrow_WhenNotificationDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            _notificationRepositoryMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync((Notification?)null);

            // Act
            Func<Task> act = async () => await service.MarkAsReadAsync(1, "user-123");

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Notification not found.");
        }

        [Fact]
        public async Task MarkAsReadAsync_ShouldThrow_WhenNotificationBelongsToAnotherUser()
        {
            // Arrange
            var service = CreateService();

            var notification = CreateNotification(1, "owner-user");

            _notificationRepositoryMock
                .Setup(r => r.GetByIdAsync(notification.Id))
                .ReturnsAsync(notification);

            // Act
            Func<Task> act = async () => await service.MarkAsReadAsync(notification.Id, "another-user");

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Notification not found.");
        }

        [Fact]
        public async Task MarkAsReadAsync_ShouldMarkNotificationAsRead_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var notification = CreateNotification(1, "user-123", isRead: false);

            _notificationRepositoryMock
                .Setup(r => r.GetByIdAsync(notification.Id))
                .ReturnsAsync(notification);

            _notificationRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.MarkAsReadAsync(notification.Id, "user-123");

            // Assert
            notification.IsRead.Should().BeTrue();
            _notificationRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task MarkAsReadAsync_ShouldDoNothing_WhenAlreadyRead()
        {
            // Arrange
            var service = CreateService();

            var notification = CreateNotification(1, "user-123", isRead: true);

            _notificationRepositoryMock
                .Setup(r => r.GetByIdAsync(notification.Id))
                .ReturnsAsync(notification);

            // Act
            await service.MarkAsReadAsync(notification.Id, "user-123");

            // Assert
            notification.IsRead.Should().BeTrue();
            _notificationRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task MarkMultipleAsReadAsync_ShouldDoNothing_WhenListIsEmpty()
        {
            // Arrange
            var service = CreateService();

            // Act
            await service.MarkMultipleAsReadAsync(new List<int>(), "user-123");

            // Assert
            _notificationRepositoryMock.Verify(
                r => r.MarkMultipleAsReadAsync(It.IsAny<List<int>>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task MarkMultipleAsReadAsync_ShouldCallRepository_WhenListHasIds()
        {
            // Arrange
            var service = CreateService();

            var ids = new List<int> { 1, 2, 3 };
            var userId = "user-123";

            _notificationRepositoryMock
                .Setup(r => r.MarkMultipleAsReadAsync(ids, userId))
                .Returns(Task.CompletedTask);

            // Act
            await service.MarkMultipleAsReadAsync(ids, userId);

            // Assert
            _notificationRepositoryMock.Verify(
                r => r.MarkMultipleAsReadAsync(ids, userId),
                Times.Once);
        }

        [Fact]
        public async Task MarkAllAsReadAsync_ShouldCallRepository()
        {
            // Arrange
            var service = CreateService();

            var userId = "user-123";

            _notificationRepositoryMock
                .Setup(r => r.MarkAllAsReadByUserIdAsync(userId))
                .Returns(Task.CompletedTask);

            // Act
            await service.MarkAllAsReadAsync(userId);

            // Assert
            _notificationRepositoryMock.Verify(
                r => r.MarkAllAsReadByUserIdAsync(userId),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrow_WhenNotificationDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            _notificationRepositoryMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync((Notification?)null);

            // Act
            Func<Task> act = async () => await service.DeleteAsync(1, "user-123");

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Notification not found.");
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteNotification_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var notification = CreateNotification(1, "user-123");

            _notificationRepositoryMock
                .Setup(r => r.GetByIdAsync(notification.Id))
                .ReturnsAsync(notification);

            _notificationRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.DeleteAsync(notification.Id, "user-123");

            // Assert
            _notificationRepositoryMock.Verify(r => r.Delete(notification), Times.Once);
            _notificationRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAllAsync_ShouldCallRepository()
        {
            // Arrange
            var service = CreateService();

            var userId = "user-123";

            _notificationRepositoryMock
                .Setup(r => r.DeleteAllByUserIdAsync(userId))
                .Returns(Task.CompletedTask);

            // Act
            await service.DeleteAllAsync(userId);

            // Assert
            _notificationRepositoryMock.Verify(
                r => r.DeleteAllByUserIdAsync(userId),
                Times.Once);
        }

        [Fact]
        public async Task SendAsync_ShouldCreateNotification()
        {
            // Arrange
            var service = CreateService();

            _notificationRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Notification>()))
                .Callback<Notification>(notification =>
                {
                    notification.Id = 1;
                })
                .Returns(Task.CompletedTask);

            _notificationRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.SendAsync(
                "user-123",
                NotificationType.LoanRequested,
                "Loan requested",
                5,
                NotificationReferenceType.Loan);

            // Assert
            _notificationRepositoryMock.Verify(r => r.AddAsync(It.Is<Notification>(n =>
                n.UserId == "user-123" &&
                n.Type == NotificationType.LoanRequested &&
                n.Message == "Loan requested" &&
                n.ReferenceId == 5 &&
                n.ReferenceType == NotificationReferenceType.Loan &&
                n.IsRead == false
            )), Times.Once);

            _notificationRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SendToMultipleAsync_ShouldSendToEachUser()
        {
            // Arrange
            var service = CreateService();

            _notificationRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Notification>()))
                .Returns(Task.CompletedTask);

            _notificationRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            var userIds = new List<string>
            {
                "user-1",
                "user-2",
                "user-3"
            };

            // Act
            await service.SendToMultipleAsync(
                userIds,
                NotificationType.LoanRequested,
                "Message",
                1,
                NotificationReferenceType.Loan);

            // Assert
            _notificationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Exactly(3));
            _notificationRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Exactly(3));
        }

        [Fact]
        public async Task SendToAdminsAsync_ShouldDoNothing_WhenThereAreNoAdmins()
        {
            // Arrange
            var service = CreateService();

            _userManagerMock
                .Setup(m => m.GetUsersInRoleAsync("Admin"))
                .ReturnsAsync(new List<ApplicationUser>());

            // Act
            await service.SendToAdminsAsync(
                NotificationType.LoanRequested,
                "Message",
                1,
                NotificationReferenceType.Loan);

            // Assert
            _notificationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Never);
        }

        [Fact]
        public async Task SendToAdminsAsync_ShouldSendNotificationToEachAdmin()
        {
            // Arrange
            var service = CreateService();

            var admins = new List<ApplicationUser>
            {
                CreateAdmin("admin-1"),
                CreateAdmin("admin-2")
            };

            _userManagerMock
                .Setup(m => m.GetUsersInRoleAsync("Admin"))
                .ReturnsAsync(admins);

            _notificationRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Notification>()))
                .Returns(Task.CompletedTask);

            _notificationRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.SendToAdminsAsync(
                NotificationType.LoanRequested,
                "Message",
                1,
                NotificationReferenceType.Loan);

            // Assert
            _notificationRepositoryMock.Verify(r => r.AddAsync(It.Is<Notification>(n =>
                n.UserId == "admin-1"
            )), Times.Once);

            _notificationRepositoryMock.Verify(r => r.AddAsync(It.Is<Notification>(n =>
                n.UserId == "admin-2"
            )), Times.Once);

            _notificationRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
        }
    }
}