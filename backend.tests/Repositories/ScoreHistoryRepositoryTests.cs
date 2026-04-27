using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Repositories
{
    public class ScoreHistoryRepositoryTests
    {
        private ApplicationDbContext CreateContext() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        private ApplicationUser MakeUser(string id, int score = 100) => new()
        {
            Id = id,
            UserName = id,
            Email = $"{id}@test.com",
            FullName = "Test User",
            Score = score
        };

        private ScoreHistory MakeEntry(string userId, int points, ScoreChangeReason reason = ScoreChangeReason.OnTimeReturn) => new()
        {
            UserId = userId,
            PointsChanged = points,
            ScoreAfterChange = 100,
            Reason = reason
        };

        [Fact]
        public async Task AddAsync_ShouldAddEntry()
        {
            var context = CreateContext();
            var repo = new ScoreHistoryRepository(context);

            context.Users.Add(MakeUser("user1"));
            await context.SaveChangesAsync();

            await repo.AddAsync(MakeEntry("user1", 5));
            await repo.SaveChangesAsync();

            Assert.Equal(1, await context.ScoreHistories.CountAsync());
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnEntry()
        {
            var context = CreateContext();
            var repo = new ScoreHistoryRepository(context);

            context.Users.Add(MakeUser("user1"));
            var entry = MakeEntry("user1", 5);
            context.ScoreHistories.Add(entry);
            await context.SaveChangesAsync();

            var result = await repo.GetByIdAsync(entry.Id);

            Assert.NotNull(result);
            Assert.Equal(entry.Id, result!.Id);
        }

        [Fact]
        public async Task GetScoreHistoryByUserIdAsync_ShouldReturnOnlyUserEntries()
        {
            var context = CreateContext();
            var repo = new ScoreHistoryRepository(context);

            context.Users.AddRange(MakeUser("user1"), MakeUser("user2"));
            context.ScoreHistories.AddRange(
                MakeEntry("user1", 5),
                MakeEntry("user1", -3),
                MakeEntry("user2", 10) //excluded
            );
            await context.SaveChangesAsync();

            var result = await repo.GetScoreHistoryByUserIdAsync("user1");

            Assert.Equal(2, result.Count);
            Assert.All(result, e => Assert.Equal("user1", e.UserId));
        }

        [Fact]
        public async Task GetScoreSummaryByUserIdAsync_ShouldSumCorrectly()
        {
            var context = CreateContext();
            var repo = new ScoreHistoryRepository(context);

            context.Users.Add(MakeUser("user1", score: 102));
            context.ScoreHistories.AddRange(
                MakeEntry("user1", 10),
                MakeEntry("user1", 5),
                MakeEntry("user1", -3)
            );
            await context.SaveChangesAsync();

            var result = await repo.GetScoreSummaryByUserIdAsync("user1");

            Assert.Equal(102, result.CurrentScore);
            Assert.Equal(15, result.TotalPointsEarned);
            Assert.Equal(-3, result.TotalPointsLost);
            Assert.Equal(3, result.TotalScoreEvents);
        }
    }
}