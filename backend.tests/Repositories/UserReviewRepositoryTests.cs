using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Repositories
{
    public class UserReviewRepositoryTests
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

        private UserReview MakeReview(string reviewerId, string reviewedUserId, int rating = 4) => new()
        {
            ReviewerId = reviewerId,
            ReviewedUserId = reviewedUserId,
            Rating = rating
        };

        [Fact]
        public async Task AddAsync_ShouldAddReview()
        {
            var context = CreateContext();
            var repo = new UserReviewRepository(context);

            context.Users.AddRange(MakeUser("user1"), MakeUser("user2"));
            await context.SaveChangesAsync();

            await repo.AddAsync(MakeReview("user1", "user2"));
            await repo.SaveChangesAsync();

            Assert.Equal(1, await context.UserReviews.CountAsync());
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnReview()
        {
            var context = CreateContext();
            var repo = new UserReviewRepository(context);

            context.Users.AddRange(MakeUser("user1"), MakeUser("user2"));
            var review = MakeReview("user1", "user2");
            context.UserReviews.Add(review);
            await context.SaveChangesAsync();

            var result = await repo.GetByIdAsync(review.Id);

            Assert.NotNull(result);
            Assert.Equal(review.Id, result!.Id);
        }

        [Fact]
        public async Task HasReviewedUserAsync_ShouldReturnTrue_WhenReviewExists()
        {
            var context = CreateContext();
            var repo = new UserReviewRepository(context);

            context.Users.AddRange(MakeUser("user1"), MakeUser("user2"));
            context.UserReviews.Add(MakeReview("user1", "user2"));
            await context.SaveChangesAsync();

            var result = await repo.HasReviewedUserAsync("user1", "user2");

            Assert.True(result);
        }

        [Fact]
        public async Task HasReviewedUserAsync_ShouldReturnFalse_WhenNoReview()
        {
            var context = CreateContext();
            var repo = new UserReviewRepository(context);

            var result = await repo.HasReviewedUserAsync("user1", "user2");

            Assert.False(result);
        }

        [Fact]
        public async Task GetRatingSummaryAsync_ShouldCalculateCorrectly()
        {
            var context = CreateContext();
            var repo = new UserReviewRepository(context);

            context.Users.AddRange(MakeUser("user1"), MakeUser("user2"), MakeUser("user3"));
            context.UserReviews.AddRange(
                MakeReview("user1", "user2", rating: 5),
                MakeReview("user3", "user2", rating: 3)
            );
            await context.SaveChangesAsync();

            var result = await repo.GetRatingSummaryAsync("user2");

            Assert.Equal(4.0, result.AverageRating);
            Assert.Equal(2, result.TotalReviews);
            Assert.Equal(1, result.Rating5Count);
            Assert.Equal(1, result.Rating3Count);
        }

        [Fact]
        public async Task GetRatingSummaryAsync_ShouldReturnEmpty_WhenNoReviews()
        {
            var context = CreateContext();
            var repo = new UserReviewRepository(context);

            var result = await repo.GetRatingSummaryAsync("user1");

            Assert.Equal(0, result.TotalReviews);
        }
    }
}