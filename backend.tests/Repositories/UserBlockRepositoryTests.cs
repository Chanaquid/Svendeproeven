using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Repositories
{
    public class UserBlockRepositoryTests
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

        private UserBlock MakeBlock(string blockerId, string blockedId) => new()
        {
            BlockerId = blockerId,
            BlockedId = blockedId
        };

        [Fact]
        public async Task AddAsync_ShouldAddBlock()
        {
            var context = CreateContext();
            var repo = new UserBlockRepository(context);

            context.Users.AddRange(MakeUser("user1"), MakeUser("user2"));
            await context.SaveChangesAsync();

            await repo.AddAsync(MakeBlock("user1", "user2"));
            await repo.SaveChangesAsync();

            Assert.Equal(1, await context.UserBlocks.CountAsync());
        }

        [Fact]
        public async Task IsBlockedAsync_ShouldReturnTrue_WhenBlockExists()
        {
            var context = CreateContext();
            var repo = new UserBlockRepository(context);

            context.Users.AddRange(MakeUser("user1"), MakeUser("user2"));
            context.UserBlocks.Add(MakeBlock("user1", "user2"));
            await context.SaveChangesAsync();

            var result = await repo.IsBlockedAsync("user1", "user2");

            Assert.True(result);
        }

        [Fact]
        public async Task IsBlockedAsync_ShouldReturnFalse_WhenNoBlock()
        {
            var context = CreateContext();
            var repo = new UserBlockRepository(context);

            var result = await repo.IsBlockedAsync("user1", "user2");

            Assert.False(result);
        }

        [Fact]
        public async Task AreBlockedEitherWayAsync_ShouldReturnTrue_WhenReverseBlockExists()
        {
            var context = CreateContext();
            var repo = new UserBlockRepository(context);

            context.Users.AddRange(MakeUser("user1"), MakeUser("user2"));
            context.UserBlocks.Add(MakeBlock("user2", "user1")); // reverse direction
            await context.SaveChangesAsync();

            var result = await repo.AreBlockedEitherWayAsync("user1", "user2");

            Assert.True(result);
        }

        [Fact]
        public async Task GetBlockedUserIdsAsync_ShouldReturnBothDirections()
        {
            var context = CreateContext();
            var repo = new UserBlockRepository(context);

            context.Users.AddRange(MakeUser("user1"), MakeUser("user2"), MakeUser("user3"));
            context.UserBlocks.AddRange(
                MakeBlock("user1", "user2"), //user1 blocked user2
                MakeBlock("user3", "user1")  //user3 blocked user1
            );
            await context.SaveChangesAsync();

            var result = await repo.GetBlockedUserIdsAsync("user1");

            Assert.Contains("user2", result);
            Assert.Contains("user3", result);
        }

        [Fact]
        public async Task GetOutgoingBlockedUserIdsAsync_ShouldReturnOnlyOutgoing()
        {
            var context = CreateContext();
            var repo = new UserBlockRepository(context);

            context.Users.AddRange(MakeUser("user1"), MakeUser("user2"), MakeUser("user3"));
            context.UserBlocks.AddRange(
                MakeBlock("user1", "user2"), //outgoing
                MakeBlock("user3", "user1")  //incoming, excluded
            );
            await context.SaveChangesAsync();

            var result = await repo.GetOutgoingBlockedUserIdsAsync("user1");

            Assert.Contains("user2", result);
            Assert.DoesNotContain("user3", result);
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveBlock()
        {
            var context = CreateContext();
            var repo = new UserBlockRepository(context);

            context.Users.AddRange(MakeUser("user1"), MakeUser("user2"));
            var block = MakeBlock("user1", "user2");
            context.UserBlocks.Add(block);
            await context.SaveChangesAsync();

            await repo.DeleteAsync(block);
            await repo.SaveChangesAsync();

            Assert.Equal(0, await context.UserBlocks.CountAsync());
        }
    }
}