using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Repositories
{
    public class SupportRepositoryTests
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

        private SupportThread MakeThread(string userId, SupportThreadStatus status = SupportThreadStatus.Open) => new()
        {
            UserId = userId,
            Subject = "Help needed",
            Status = status
        };

        [Fact]
        public async Task AddThreadAsync_ShouldAddThread()
        {
            var context = CreateContext();
            var repo = new SupportRepository(context);

            context.Users.Add(MakeUser("user1"));
            await context.SaveChangesAsync();

            await repo.AddThreadAsync(MakeThread("user1"));
            await repo.SaveChangesAsync();

            Assert.Equal(1, await context.SupportThreads.CountAsync());
        }

        [Fact]
        public async Task GetThreadByIdAsync_ShouldReturnThread()
        {
            var context = CreateContext();
            var repo = new SupportRepository(context);

            context.Users.Add(MakeUser("user1"));
            var thread = MakeThread("user1");
            context.SupportThreads.Add(thread);
            await context.SaveChangesAsync();

            var result = await repo.GetThreadByIdAsync(thread.Id);

            Assert.NotNull(result);
            Assert.Equal(thread.Id, result!.Id);
        }

        [Fact]
        public async Task HasActiveThreadAsync_ShouldReturnTrue_WhenOpenThreadExists()
        {
            var context = CreateContext();
            var repo = new SupportRepository(context);

            context.Users.Add(MakeUser("user1"));
            context.SupportThreads.Add(MakeThread("user1", SupportThreadStatus.Open));
            await context.SaveChangesAsync();

            var result = await repo.HasActiveThreadAsync("user1");

            Assert.True(result);
        }

        [Fact]
        public async Task HasActiveThreadAsync_ShouldReturnFalse_WhenOnlyClosedExists()
        {
            var context = CreateContext();
            var repo = new SupportRepository(context);

            context.Users.Add(MakeUser("user1"));
            context.SupportThreads.Add(MakeThread("user1", SupportThreadStatus.Closed));
            await context.SaveChangesAsync();

            var result = await repo.HasActiveThreadAsync("user1");

            Assert.False(result);
        }

        [Fact]
        public async Task GetActiveThreadByUserIdAsync_ShouldReturnNonClosedThread()
        {
            var context = CreateContext();
            var repo = new SupportRepository(context);

            context.Users.Add(MakeUser("user1"));
            context.SupportThreads.AddRange(
                MakeThread("user1", SupportThreadStatus.Claimed),
                MakeThread("user1", SupportThreadStatus.Closed) //excluded
            );
            await context.SaveChangesAsync();

            var result = await repo.GetActiveThreadByUserIdAsync("user1");

            Assert.NotNull(result);
            Assert.NotEqual(SupportThreadStatus.Closed, result!.Status);
        }

        [Fact]
        public async Task GetUnreadCountAsync_ShouldCountMessagesNotFromUser()
        {
            var context = CreateContext();
            var repo = new SupportRepository(context);

            context.Users.AddRange(MakeUser("user1"), MakeUser("admin1"));
            var thread = MakeThread("user1");
            context.SupportThreads.Add(thread);
            await context.SaveChangesAsync();

            context.SupportMessages.AddRange(
                new SupportMessage { SupportThreadId = thread.Id, SenderId = "admin1", Content = "Hi", IsRead = false },
                new SupportMessage { SupportThreadId = thread.Id, SenderId = "admin1", Content = "Hi", IsRead = false },
                new SupportMessage { SupportThreadId = thread.Id, SenderId = "user1", Content = "Hi", IsRead = false }, // own, excluded
                new SupportMessage { SupportThreadId = thread.Id, SenderId = "admin1", Content = "Hi", IsRead = true }  // read, excluded
            );
            await context.SaveChangesAsync();

            var result = await repo.GetUnreadCountAsync(thread.Id, "user1");

            Assert.Equal(2, result);
        }

        [Fact]
        public async Task AddMessageAsync_ShouldAddMessage()
        {
            var context = CreateContext();
            var repo = new SupportRepository(context);

            context.Users.AddRange(MakeUser("user1"), MakeUser("admin1"));
            var thread = MakeThread("user1");
            context.SupportThreads.Add(thread);
            await context.SaveChangesAsync();

            await repo.AddMessageAsync(new SupportMessage
            {
                SupportThreadId = thread.Id,
                SenderId = "admin1",
                Content = "We're looking into it"
            });
            await repo.SaveChangesAsync();

            Assert.Equal(1, await context.SupportMessages.CountAsync());
        }
    }
}