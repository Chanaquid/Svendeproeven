using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Repositories
{
    public class UserFavoriteRepositoryTests
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

        private UserFavoriteItem MakeFavorite(string userId, int itemId, bool notify = false) => new()
        {
            UserId = userId,
            ItemId = itemId,
            NotifyWhenAvailable = notify
        };

        [Fact]
        public async Task AddAsync_ShouldAddFavorite()
        {
            var context = CreateContext();
            var repo = new UserFavoriteRepository(context);

            context.Users.Add(MakeUser("user1"));
            await context.SaveChangesAsync();

            await repo.AddAsync(MakeFavorite("user1", 10));
            await repo.SaveChangesAsync();

            Assert.Equal(1, await context.UserFavoriteItems.CountAsync());
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_WhenFavoriteExists()
        {
            var context = CreateContext();
            var repo = new UserFavoriteRepository(context);

            context.Users.Add(MakeUser("user1"));
            context.UserFavoriteItems.Add(MakeFavorite("user1", 10));
            await context.SaveChangesAsync();

            var result = await repo.ExistsAsync("user1", 10);

            Assert.True(result);
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnFalse_WhenFavoriteDoesNotExist()
        {
            var context = CreateContext();
            var repo = new UserFavoriteRepository(context);

            var result = await repo.ExistsAsync("user1", 10);

            Assert.False(result);
        }

        [Fact]
        public async Task GetUsersToNotifyAsync_ShouldReturnOnlyNotifyEnabled()
        {
            var context = CreateContext();
            var repo = new UserFavoriteRepository(context);

            context.Users.AddRange(MakeUser("user1"), MakeUser("user2"), MakeUser("user3"));
            context.UserFavoriteItems.AddRange(
                MakeFavorite("user1", 10, notify: true),
                MakeFavorite("user2", 10, notify: true),
                MakeFavorite("user3", 10, notify: false) // excluded
            );
            await context.SaveChangesAsync();

            var result = await repo.GetUsersToNotifyAsync(10);

            Assert.Equal(2, result.Count);
            Assert.DoesNotContain("user3", result);
        }

        [Fact]
        public async Task Remove_ShouldDeleteFavorite()
        {
            var context = CreateContext();
            var repo = new UserFavoriteRepository(context);

            context.Users.Add(MakeUser("user1"));
            var fav = MakeFavorite("user1", 10);
            context.UserFavoriteItems.Add(fav);
            await context.SaveChangesAsync();

            repo.Remove(fav);
            await repo.SaveChangesAsync();

            Assert.Equal(0, await context.UserFavoriteItems.CountAsync());
        }

        [Fact]
        public async Task GetAllByItemIdAsync_ShouldReturnAllFavoritesForItem()
        {
            var context = CreateContext();
            var repo = new UserFavoriteRepository(context);

            context.Users.AddRange(MakeUser("user1"), MakeUser("user2"), MakeUser("user3"));
            context.UserFavoriteItems.AddRange(
                MakeFavorite("user1", 10),
                MakeFavorite("user2", 10),
                MakeFavorite("user3", 99) //different item
            );
            await context.SaveChangesAsync();

            var result = await repo.GetAllByItemIdAsync(10);

            Assert.Equal(2, result.Count);
        }
    }
}