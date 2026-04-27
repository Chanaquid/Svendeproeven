using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Repositories
{
    public class ItemRepositoryTests
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

        private Category MakeCategory(int id = 1) => new Category
        {
            Id = id,
            Name = "Test Category"
        };

        private Item MakeItem(int id, string ownerId, int categoryId, ItemStatus status = ItemStatus.Approved) => new Item
        {
            Id = id,
            RowVersion = new byte[] { 1 },
            OwnerId = ownerId,
            CategoryId = categoryId,
            Title = $"Item {id}",
            Slug = $"item-{id}",
            Description = "Test description",
            QrCode = $"QR{id:D8}",
            Status = status,
            IsActive = true,
            Availability = ItemAvailability.Available,
            AvailableFrom = DateTime.UtcNow.AddDays(-1),
            AvailableUntil = DateTime.UtcNow.AddDays(30),
            PickupAddress = "Test Address",
            CurrentValue = 100,
            PricePerDay = 10
        };


        [Fact]
        public async Task AddAsync_ShouldAddItem()
        {
            var context = CreateContext();
            var repo = new ItemRepository(context);

            context.Users.Add(MakeUser("owner1"));
            context.Categories.Add(MakeCategory());
            await context.SaveChangesAsync();

            await repo.AddAsync(MakeItem(1, "owner1", 1));
            await repo.SaveChangesAsync();

            var result = await context.Items.FirstOrDefaultAsync();

            Assert.NotNull(result);
            Assert.Equal("Item 1", result!.Title);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnItem()
        {
            var context = CreateContext();
            var repo = new ItemRepository(context);

            context.Users.Add(MakeUser("owner1"));
            context.Categories.Add(MakeCategory());
            context.Items.Add(MakeItem(1, "owner1", 1));
            await context.SaveChangesAsync();

            var result = await repo.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result!.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_ForNonExistentItem()
        {
            var context = CreateContext();
            var repo = new ItemRepository(context);

            var result = await repo.GetByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task SlugExistsAsync_ShouldReturnTrue_WhenSlugExists()
        {
            var context = CreateContext();
            var repo = new ItemRepository(context);

            context.Users.Add(MakeUser("owner1"));
            context.Categories.Add(MakeCategory());
            context.Items.Add(MakeItem(1, "owner1", 1));
            await context.SaveChangesAsync();

            var result = await repo.SlugExistsAsync("item-1");

            Assert.True(result);
        }

        [Fact]
        public async Task SlugExistsAsync_ShouldReturnFalse_WhenSlugDoesNotExist()
        {
            var context = CreateContext();
            var repo = new ItemRepository(context);

            var result = await repo.SlugExistsAsync("nonexistent-slug");

            Assert.False(result);
        }

        [Fact]
        public async Task QrCodeExistsAsync_ShouldReturnTrue_WhenQrCodeExists()
        {
            var context = CreateContext();
            var repo = new ItemRepository(context);

            context.Users.Add(MakeUser("owner1"));
            context.Categories.Add(MakeCategory());
            context.Items.Add(MakeItem(1, "owner1", 1));
            await context.SaveChangesAsync();

            var result = await repo.QrCodeExistsAsync("QR00000001");

            Assert.True(result);
        }

        [Fact]
        public async Task IsOwnerAsync_ShouldReturnTrue_WhenUserOwnsItem()
        {
            var context = CreateContext();
            var repo = new ItemRepository(context);

            context.Users.Add(MakeUser("owner1"));
            context.Categories.Add(MakeCategory());
            context.Items.Add(MakeItem(1, "owner1", 1));
            await context.SaveChangesAsync();

            var result = await repo.IsOwnerAsync(1, "owner1");

            Assert.True(result);
        }

        [Fact]
        public async Task IsOwnerAsync_ShouldReturnFalse_WhenUserDoesNotOwnItem()
        {
            var context = CreateContext();
            var repo = new ItemRepository(context);

            context.Users.Add(MakeUser("owner1"));
            context.Categories.Add(MakeCategory());
            context.Items.Add(MakeItem(1, "owner1", 1));
            await context.SaveChangesAsync();

            var result = await repo.IsOwnerAsync(1, "other-user");

            Assert.False(result);
        }

        [Fact]
        public async Task GetPendingApprovalsCountAsync_ShouldReturnCorrectCount()
        {
            var context = CreateContext();
            var repo = new ItemRepository(context);

            context.Users.Add(MakeUser("owner1"));
            context.Categories.Add(MakeCategory());
            context.Items.AddRange(
                MakeItem(1, "owner1", 1, ItemStatus.Pending),
                MakeItem(2, "owner1", 1, ItemStatus.Pending),
                MakeItem(3, "owner1", 1, ItemStatus.Approved) // excluded
            );
            await context.SaveChangesAsync();

            var result = await repo.GetPendingApprovalsCountAsync();

            Assert.Equal(2, result);
        }

        [Fact]
        public async Task GetAvailableCountAsync_ShouldOnlyCountApprovedActiveAvailableItems()
        {
            var context = CreateContext();
            var repo = new ItemRepository(context);

            context.Users.Add(MakeUser("owner1"));
            context.Categories.Add(MakeCategory());

            var available = MakeItem(1, "owner1", 1, ItemStatus.Approved);

            var onRent = MakeItem(2, "owner1", 1, ItemStatus.Approved);
            onRent.Availability = ItemAvailability.OnRent;

            var pending = MakeItem(3, "owner1", 1, ItemStatus.Pending);

            var deleted = MakeItem(4, "owner1", 1, ItemStatus.Approved);
            deleted.IsDeleted = true;

            context.Items.AddRange(available, onRent, pending, deleted);
            await context.SaveChangesAsync();

            var result = await repo.GetAvailableCountAsync();

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task Delete_ShouldSoftDeleteItem()
        {
            var context = CreateContext();
            var repo = new ItemRepository(context);

            context.Users.Add(MakeUser("owner1"));
            context.Categories.Add(MakeCategory());
            var item = MakeItem(1, "owner1", 1);
            context.Items.Add(item);
            await context.SaveChangesAsync();

            repo.Delete(item);
            await repo.SaveChangesAsync();

            var result = await context.Items.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Id == 1);

            Assert.NotNull(result);
            Assert.True(result!.IsDeleted);
            Assert.NotNull(result.DeletedAt);
        }

        [Fact]
        public async Task GetByOwnerIdAsync_ShouldReturnOnlyNonDeletedItems()
        {
            var context = CreateContext();
            var repo = new ItemRepository(context);

            context.Users.Add(MakeUser("owner1"));
            context.Categories.Add(MakeCategory());

            var active = MakeItem(1, "owner1", 1);
            var deleted = MakeItem(2, "owner1", 1);
            deleted.IsDeleted = true;

            context.Items.AddRange(active, deleted);
            await context.SaveChangesAsync();

            var result = await repo.GetByOwnerIdAsync("owner1");

            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
        }

        [Fact]
        public async Task GetByOwnerIdAsync_ShouldReturnEmpty_WhenOwnerHasNoItems()
        {
            var context = CreateContext();
            var repo = new ItemRepository(context);

            var result = await repo.GetByOwnerIdAsync("nonexistent-owner");

            Assert.Empty(result);
        }
    }
}