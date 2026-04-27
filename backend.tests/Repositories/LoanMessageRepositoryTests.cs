using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Repositories
{
    public class LoanMessageRepositoryTests
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

        private LoanMessage MakeMessage(int loanId, string senderId, bool isRead = false) => new()
        {
            LoanId = loanId,
            SenderId = senderId,
            Content = "Hello",
            IsRead = isRead,
            SentAt = DateTime.UtcNow
        };

        [Fact]
        public async Task AddAsync_ShouldAddMessage()
        {
            var context = CreateContext();
            var repo = new LoanMessageRepository(context);

            context.Users.Add(MakeUser("user1"));
            await context.SaveChangesAsync();

            await repo.AddAsync(MakeMessage(1, "user1"));
            await repo.SaveChangesAsync();

            Assert.Equal(1, await context.LoanMessages.CountAsync());
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnMessage()
        {
            var context = CreateContext();
            var repo = new LoanMessageRepository(context);

            context.Users.Add(MakeUser("user1"));
            var msg = MakeMessage(1, "user1");
            context.LoanMessages.Add(msg);
            await context.SaveChangesAsync();

            var result = await repo.GetByIdAsync(msg.Id);

            Assert.NotNull(result);
            Assert.Equal(msg.Id, result!.Id);
        }

        [Fact]
        public async Task GetLastMessageAsync_ShouldReturnMostRecentMessage()
        {
            var context = CreateContext();
            var repo = new LoanMessageRepository(context);

            context.Users.Add(MakeUser("user1"));

            var old = MakeMessage(1, "user1");
            old.SentAt = DateTime.UtcNow.AddMinutes(-10);

            var recent = MakeMessage(1, "user1");
            recent.SentAt = DateTime.UtcNow;

            context.LoanMessages.AddRange(old, recent);
            await context.SaveChangesAsync();

            var result = await repo.GetLastMessageAsync(1);

            Assert.NotNull(result);
            Assert.Equal(recent.Id, result!.Id);
        }

        [Fact]
        public async Task GetUnreadCountAsync_ShouldCountMessagesNotFromUser()
        {
            var context = CreateContext();
            var repo = new LoanMessageRepository(context);

            context.Users.AddRange(MakeUser("user1"), MakeUser("user2"));
            context.LoanMessages.AddRange(
                MakeMessage(1, "user2", isRead: false), //unread from other
                MakeMessage(1, "user2", isRead: false), //unread from other
                MakeMessage(1, "user1", isRead: false), //own message, excluded
                MakeMessage(1, "user2", isRead: true)   //already read, excluded
            );
            await context.SaveChangesAsync();

            var result = await repo.GetUnreadCountAsync(1, "user1");

            Assert.Equal(2, result);
        }

        [Fact]
        public async Task GetUnreadMessagesForUserAsync_ShouldReturnUnreadFromOthers()
        {
            var context = CreateContext();
            var repo = new LoanMessageRepository(context);

            context.Users.AddRange(MakeUser("user1"), MakeUser("user2"));
            context.LoanMessages.AddRange(
                MakeMessage(1, "user2", isRead: false),
                MakeMessage(1, "user1", isRead: false) //own message, excluded
            );
            await context.SaveChangesAsync();

            var result = await repo.GetUnreadMessagesForUserAsync(1, "user1");

            Assert.Single(result);
            Assert.Equal("user2", result[0].SenderId);
        }
    }
}