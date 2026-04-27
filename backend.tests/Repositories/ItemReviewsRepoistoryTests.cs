using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Repositories
{
    public class ItemReviewRepositoryTests
    {
        private ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private ApplicationUser MakeUser(string id) => new ApplicationUser
        {
            Id = id,
            UserName = id,
            Email = $"{id}@test.com",
            FullName = "Test User"
        };

        private ItemReview MakeReview(int id, int itemId, string reviewerId, int rating = 4, int? loanId = null) => new ItemReview
        {
            Id = id,
            ItemId = itemId,
            ReviewerId = reviewerId,
            Rating = rating,
            LoanId = loanId,
            Comment = "Great item",
            IsDeleted = false
        };

        [Fact]
        public async Task AddItemReviewAsync_ShouldAddReview()
        {
            var context = CreateContext();
            var repo = new ItemReviewRepository(context);

            context.Users.Add(MakeUser("user1"));
            await context.SaveChangesAsync();

            var review = MakeReview(1, 10, "user1", rating: 5);

            await repo.AddItemReviewAsync(review);
            await repo.SaveChangesAsync();

            var result = await context.ItemReviews.FirstOrDefaultAsync();

            Assert.NotNull(result);
            Assert.Equal(5, result!.Rating);
        }

        [Fact]
        public async Task GetItemReviewByIdAsync_ShouldReturnReview()
        {
            var context = CreateContext();
            var repo = new ItemReviewRepository(context);

            context.Users.Add(MakeUser("user1"));
            context.ItemReviews.Add(MakeReview(1, 10, "user1"));
            await context.SaveChangesAsync();

            var result = await repo.GetItemReviewByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result!.Id);
        }

        [Fact]
        public async Task GetItemReviewByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            var context = CreateContext();
            var repo = new ItemReviewRepository(context);

            var result = await repo.GetItemReviewByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetItemReviewByLoanIdAsync_ShouldReturnReview_WhenLoanMatches()
        {
            var context = CreateContext();
            var repo = new ItemReviewRepository(context);

            context.Users.Add(MakeUser("user1"));
            context.ItemReviews.Add(MakeReview(1, 10, "user1", loanId: 42));
            await context.SaveChangesAsync();

            var result = await repo.GetItemReviewByLoanIdAsync(42);

            Assert.NotNull(result);
            Assert.Equal(42, result!.LoanId);
        }

        [Fact]
        public async Task GetItemReviewByLoanIdAsync_ShouldReturnNull_WhenLoanNotFound()
        {
            var context = CreateContext();
            var repo = new ItemReviewRepository(context);

            var result = await repo.GetItemReviewByLoanIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetRatingsByItemIdAsync_ShouldReturnOnlyNonDeletedRatings()
        {
            var context = CreateContext();
            var repo = new ItemReviewRepository(context);

            context.Users.Add(MakeUser("user1"));

            var active = MakeReview(1, 10, "user1", rating: 5);
            var deleted = MakeReview(2, 10, "user1", rating: 1);
            deleted.IsDeleted = true;

            context.ItemReviews.AddRange(active, deleted);
            await context.SaveChangesAsync();

            var result = await repo.GetRatingsByItemIdAsync(10);

            Assert.Single(result);
            Assert.Equal(5, result[0]);
        }

        [Fact]
        public async Task GetRatingsByItemIdAsync_ShouldReturnEmpty_WhenNoReviewsForItem()
        {
            var context = CreateContext();
            var repo = new ItemReviewRepository(context);

            var result = await repo.GetRatingsByItemIdAsync(999);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRatingsByItemIdAsync_ShouldReturnAllRatings_ForItem()
        {
            var context = CreateContext();
            var repo = new ItemReviewRepository(context);

            context.Users.Add(MakeUser("user1"));
            context.ItemReviews.AddRange(
                MakeReview(1, 10, "user1", rating: 3),
                MakeReview(2, 10, "user1", rating: 5),
                MakeReview(3, 99, "user1", rating: 1) //different item, excluded
            );
            await context.SaveChangesAsync();

            var result = await repo.GetRatingsByItemIdAsync(10);

            Assert.Equal(2, result.Count);
            Assert.Contains(3, result);
            Assert.Contains(5, result);
        }

        [Fact]
        public async Task MarkReviewsDeletedByItemId_ShouldSoftDeleteAllReviewsForItem()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            //Disable FK enforcement so we don't need to seed a real Item with RowVersion
            using var pragmaCmd = connection.CreateCommand();
            pragmaCmd.CommandText = "PRAGMA foreign_keys = OFF;";
            pragmaCmd.ExecuteNonQuery();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            var repo = new ItemReviewRepository(context);

            context.Users.Add(MakeUser("user1"));
            await context.SaveChangesAsync();

            context.ItemReviews.AddRange(
                MakeReview(1, 10, "user1"),
                MakeReview(2, 10, "user1"),
                MakeReview(3, 10, "user1")
            );
            await context.SaveChangesAsync();

            repo.MarkReviewsDeletedByItemId(10);

            context.ChangeTracker.Clear();

            var results = await context.ItemReviews
                .IgnoreQueryFilters()
                .Where(r => r.ItemId == 10)
                .ToListAsync();

            Assert.Equal(3, results.Count);
            Assert.All(results, r => Assert.True(r.IsDeleted));
            Assert.All(results, r => Assert.NotNull(r.DeletedAt));
        }

    }
}