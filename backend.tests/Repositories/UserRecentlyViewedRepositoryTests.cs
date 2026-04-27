using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Repositories
{
    public class UserRecentlyViewedRepositoryTests
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

        private UserRecentlyViewedItem MakeViewed(string userId, int itemId, DateTime? viewedAt = null) => new()
        {
            UserId = userId,
            ItemId = itemId,
            ViewedAt = viewedAt ?? DateTime.UtcNow
        };

        [Fact]
        public async Task AddAsync_ShouldAddEntry()
        {
            var context = CreateContext();
            var repo = new UserRecentlyViewedRepository(context);

            context.Users.Add(MakeUser("user1"));
            await context.SaveChangesAsync();

            await repo.AddAsync(MakeViewed("user1", 10));
            await repo.SaveChangesAsync();

            Assert.Equal(1, await context.UserRecentlyViewedItems.CountAsync());
        }

        [Fact]
        public async Task GetAsync_ShouldReturnEntry_WhenExists()
        {
            var context = CreateContext();
            var repo = new UserRecentlyViewedRepository(context);

            context.Users.Add(MakeUser("user1"));
            context.UserRecentlyViewedItems.Add(MakeViewed("user1", 10));
            await context.SaveChangesAsync();

            var result = await repo.GetAsync("user1", 10);

            Assert.NotNull(result);
            Assert.Equal(10, result!.ItemId);
        }

        [Fact]
        public async Task GetAsync_ShouldReturnNull_WhenNotFound()
        {
            var context = CreateContext();
            var repo = new UserRecentlyViewedRepository(context);

            var result = await repo.GetAsync("user1", 999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetCountByUserIdAsync_ShouldReturnCorrectCount()
        {
            var context = CreateContext();
            var repo = new UserRecentlyViewedRepository(context);

            context.Users.Add(MakeUser("user1"));
            context.UserRecentlyViewedItems.AddRange(
                MakeViewed("user1", 1),
                MakeViewed("user1", 2),
                MakeViewed("user1", 3)
            );
            await context.SaveChangesAsync();

            var result = await repo.GetCountByUserIdAsync("user1");

            Assert.Equal(3, result);
        }

        [Fact]
        public async Task DeleteOldestAsync_ShouldRemoveOldestEntry()
        {
            var context = CreateContext();
            var repo = new UserRecentlyViewedRepository(context);

            context.Users.Add(MakeUser("user1"));
            context.UserRecentlyViewedItems.AddRange(
                MakeViewed("user1", 1, DateTime.UtcNow.AddDays(-5)), //oldest
                MakeViewed("user1", 2, DateTime.UtcNow.AddDays(-1)),
                MakeViewed("user1", 3, DateTime.UtcNow)
            );
            await context.SaveChangesAsync();

            await repo.DeleteOldestAsync("user1");
            await repo.SaveChangesAsync();

            var remaining = await context.UserRecentlyViewedItems
                .Where(r => r.UserId == "user1")
                .ToListAsync();

            Assert.Equal(2, remaining.Count);
            Assert.DoesNotContain(remaining, r => r.ItemId == 1);
        }
    }
}