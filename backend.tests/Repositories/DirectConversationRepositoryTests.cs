using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Repositories
{
    public class DirectConversationRepositoryTests
    {
        private ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new ApplicationDbContext(options);
        }

        private ApplicationUser User(string id)
        {
            return new ApplicationUser
            {
                Id = id,
                UserName = id,
                FullName = id
            };
        }

        [Fact]
        public async Task GetConversationBetweenUsersAsync_ReturnsSameConversation_RegardlessOfOrder()
        {
            using var ctx = CreateContext(nameof(GetConversationBetweenUsersAsync_ReturnsSameConversation_RegardlessOfOrder));

            ctx.Users.AddRange(User("a"), User("b"));

            ctx.DirectConversations.Add(new DirectConversation
            {
                Id = 1,
                InitiatedById = "a",
                OtherUserId = "b",
                CreatedAt = DateTime.UtcNow
            });

            await ctx.SaveChangesAsync();

            var repo = new DirectConversationRepository(ctx);

            var result1 = await repo.GetConversationBetweenUsersAsync("a", "b");
            var result2 = await repo.GetConversationBetweenUsersAsync("b", "a");

            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(result1!.Id, result2!.Id);
        }

        [Fact]
        public async Task CreateAsync_CreatesConversation_WithConsistentOrdering()
        {
            using var ctx = CreateContext(nameof(CreateAsync_CreatesConversation_WithConsistentOrdering));

            ctx.Users.AddRange(User("z"), User("a"));
            await ctx.SaveChangesAsync();

            var repo = new DirectConversationRepository(ctx);

            var convo = await repo.CreateAsync("z", "a");

            Assert.Equal("a", convo.InitiatedById); // smaller id
            Assert.Equal("z", convo.OtherUserId);
            Assert.Equal(1, await ctx.DirectConversations.CountAsync());
        }

        [Fact]
        public async Task GetBlockedUserIdsAsync_ReturnsBothDirections()
        {
            using var ctx = CreateContext(nameof(GetBlockedUserIdsAsync_ReturnsBothDirections));

            ctx.UserBlocks.AddRange(
                new UserBlock { BlockerId = "u1", BlockedId = "u2" },
                new UserBlock { BlockerId = "u3", BlockedId = "u1" }
            );

            await ctx.SaveChangesAsync();

            var repo = new DirectConversationRepository(ctx);

            var result = await repo.GetBlockedUserIdsAsync("u1");

            Assert.Contains("u2", result);
            Assert.Contains("u3", result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task AreUsersBlockedAsync_ReturnsTrue_WhenEitherDirectionExists()
        {
            using var ctx = CreateContext(nameof(AreUsersBlockedAsync_ReturnsTrue_WhenEitherDirectionExists));

            ctx.UserBlocks.Add(new UserBlock
            {
                BlockerId = "u1",
                BlockedId = "u2"
            });

            await ctx.SaveChangesAsync();

            var repo = new DirectConversationRepository(ctx);

            var result1 = await repo.AreUsersBlockedAsync("u1", "u2");
            var result2 = await repo.AreUsersBlockedAsync("u2", "u1");

            Assert.True(result1);
            Assert.True(result2);
        }

        [Fact]
        public async Task GetUnreadCountsForUserAsync_ReturnsGroupedCounts()
        {
            using var ctx = CreateContext(nameof(GetUnreadCountsForUserAsync_ReturnsGroupedCounts));

            ctx.Users.AddRange(User("u1"), User("u2"));

            var convo = new DirectConversation
            {
                Id = 1,
                InitiatedById = "u1",
                OtherUserId = "u2",
                HiddenForInitiator = false,
                HiddenForOther = false
            };

            ctx.DirectConversations.Add(convo);

            ctx.DirectMessages.AddRange(
                new DirectMessage
                {
                    Id = 1,
                    ConversationId = 1,
                    SenderId = "u2",
                    IsRead = false
                },
                new DirectMessage
                {
                    Id = 2,
                    ConversationId = 1,
                    SenderId = "u2",
                    IsRead = false
                },
                new DirectMessage
                {
                    Id = 3,
                    ConversationId = 1,
                    SenderId = "u1", //should not count
                    IsRead = false
                }
            );

            await ctx.SaveChangesAsync();

            var repo = new DirectConversationRepository(ctx);

            var result = await repo.GetUnreadCountsForUserAsync("u1");

            Assert.True(result.ContainsKey(1));
            Assert.Equal(2, result[1]);
        }

        [Fact]
        public async Task IsBlockedByCurrentUserAsync_ReturnsTrue_WhenBlocked()
        {
            using var ctx = CreateContext(nameof(IsBlockedByCurrentUserAsync_ReturnsTrue_WhenBlocked));

            ctx.UserBlocks.Add(new UserBlock
            {
                BlockerId = "u1",
                BlockedId = "u2"
            });

            await ctx.SaveChangesAsync();

            var repo = new DirectConversationRepository(ctx);

            var result = await repo.IsBlockedByCurrentUserAsync("u1", "u2");

            Assert.True(result);
        }
    }
}