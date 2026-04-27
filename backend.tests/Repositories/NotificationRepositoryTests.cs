using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Repositories
{
    public class NotificationRepositoryTests
    {
        private ApplicationDbContext CreateContext() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        private ApplicationUser MakeUser(string id) => new()
        {
            Id = id,
            UserName = id,
            Email = $"{id}@test.com",
            FullName = "Test User"
        };

        private Notification MakeNotification(string userId, bool isRead = false) => new()
        {
            UserId = userId,
            Type = NotificationType.LoanApproved,
            Message = "Test notification",
            IsRead = isRead
        };

        [Fact]
        public async Task AddAsync_ShouldAddNotification()
        {
            var context = CreateContext();
            var repo = new NotificationRepository(context);

            context.Users.Add(MakeUser("user1"));
            await context.SaveChangesAsync();

            await repo.AddAsync(MakeNotification("user1"));
            await repo.SaveChangesAsync();

            Assert.Equal(1, await context.Notifications.CountAsync());
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNotification()
        {
            var context = CreateContext();
            var repo = new NotificationRepository(context);

            context.Users.Add(MakeUser("user1"));
            var notif = MakeNotification("user1");
            context.Notifications.Add(notif);
            await context.SaveChangesAsync();

            var result = await repo.GetByIdAsync(notif.Id);

            Assert.NotNull(result);
            Assert.Equal(notif.Id, result!.Id);
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldReturnOnlyUserNotifications()
        {
            var context = CreateContext();
            var repo = new NotificationRepository(context);

            context.Users.AddRange(MakeUser("user1"), MakeUser("user2"));
            context.Notifications.AddRange(
                MakeNotification("user1"),
                MakeNotification("user1"),
                MakeNotification("user2") // excluded
            );
            await context.SaveChangesAsync();

            var result = await repo.GetByUserIdAsync("user1");

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task Delete_ShouldRemoveNotification()
        {
            var context = CreateContext();
            var repo = new NotificationRepository(context);

            context.Users.Add(MakeUser("user1"));
            var notif = MakeNotification("user1");
            context.Notifications.Add(notif);
            await context.SaveChangesAsync();

            repo.Delete(notif);
            await repo.SaveChangesAsync();

            Assert.Equal(0, await context.Notifications.CountAsync());
        }
    }
}