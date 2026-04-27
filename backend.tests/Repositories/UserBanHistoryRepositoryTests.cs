using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Repositories
{
    public class UserBanHistoryRepositoryTests
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

        private UserBanHistory MakeBan(string userId, string adminId, bool isBanned = true) => new()
        {
            UserId = userId,
            AdminId = adminId,
            IsBanned = isBanned,
            Reason = "Violated rules",
            BannedAt = DateTime.UtcNow
        };

        [Fact]
        public async Task AddAsync_ShouldAddBanRecord()
        {
            var context = CreateContext();
            var repo = new UserBanHistoryRepository(context);

            context.Users.AddRange(MakeUser("user1"), MakeUser("admin1"));
            await context.SaveChangesAsync();

            await repo.AddAsync(MakeBan("user1", "admin1"));
            await repo.SaveChangesAsync();

            Assert.Equal(1, await context.UserBanHistories.CountAsync());
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnBanRecord()
        {
            var context = CreateContext();
            var repo = new UserBanHistoryRepository(context);

            context.Users.AddRange(MakeUser("user1"), MakeUser("admin1"));
            var ban = MakeBan("user1", "admin1");
            context.UserBanHistories.Add(ban);
            await context.SaveChangesAsync();

            var result = await repo.GetByIdAsync(ban.Id);

            Assert.NotNull(result);
            Assert.Equal(ban.Id, result!.Id);
        }

        [Fact]
        public async Task GetLatestByUserIdAsync_ShouldReturnMostRecentBan()
        {
            var context = CreateContext();
            var repo = new UserBanHistoryRepository(context);

            context.Users.AddRange(MakeUser("user1"), MakeUser("admin1"));

            var old = MakeBan("user1", "admin1");
            old.BannedAt = DateTime.UtcNow.AddDays(-5);

            var recent = MakeBan("user1", "admin1");
            recent.BannedAt = DateTime.UtcNow;

            context.UserBanHistories.AddRange(old, recent);
            await context.SaveChangesAsync();

            var result = await repo.GetLatestByUserIdAsync("user1");

            Assert.NotNull(result);
            Assert.True(result!.BannedAt >= old.BannedAt);
        }

        [Fact]
        public async Task GetLatestByUserIdAsync_ShouldReturnNull_WhenNoBanHistory()
        {
            var context = CreateContext();
            var repo = new UserBanHistoryRepository(context);

            var result = await repo.GetLatestByUserIdAsync("nonexistent");

            Assert.Null(result);
        }
    }
}