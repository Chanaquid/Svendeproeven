using backend.Data;
using backend.Models;
using backend.Repositories;
using backend.Dtos;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Repositories
{
    public class TestApplicationDbContext : ApplicationDbContext
    {
        public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //SQLite cannot auto-generate rowversion — treat it as a plain column
            modelBuilder.Entity<Item>()
                .Property(i => i.RowVersion)
                .IsRowVersion()
                .ValueGeneratedNever();
        }
    }

    public class CategoryRepositoryTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _context;
        private readonly CategoryRepository _repo;

        public CategoryRepositoryTests()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new TestApplicationDbContext(options);
            _context.Database.EnsureCreated();

            _repo = new CategoryRepository(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Close();
        }

        //Helpers

        private ApplicationUser CreateUser(string id)
        {
            return new ApplicationUser
            {
                Id = id,
                UserName = id,
                Email = $"{id}@test.com",
                FullName = $"User {id}"
            };
        }

        private Category CreateCategory(int id, string name, bool isActive = true)
        {
            return new Category
            {
                Id = id,
                Name = name,
                Slug = name.ToLower(),
                IsActive = isActive
            };
        }

        private Item CreateItem(int id, int categoryId, bool isActive = true, ItemStatus status = ItemStatus.Approved)
        {
            return new Item
            {
                Id = id,
                Title = "Item",
                Slug = $"item-{id}",
                CategoryId = categoryId,
                QrCode = $"QR{id:D10}",
                OwnerId = "u1",
                IsActive = isActive,
                Status = status,
                Availability = ItemAvailability.Available,
                Condition = ItemCondition.Good,
                RowVersion = new byte[8]
            };
        }

        //GetAllAsync

        [Fact]
        public async Task GetAllAsync_Should_ReturnOnlyActive_ForNonAdmin()
        {
            _context.Categories.AddRange(
                CreateCategory(1, "Active", true),
                CreateCategory(2, "Inactive", false)
            );
            await _context.SaveChangesAsync();

            var result = await _repo.GetAllAsync(false);

            Assert.Single(result);
            Assert.Equal("Active", result[0].Name);
        }

        [Fact]
        public async Task GetAllAsync_Should_ReturnAll_ForAdmin()
        {
            _context.Categories.AddRange(
                CreateCategory(1, "A", true),
                CreateCategory(2, "B", false)
            );
            await _context.SaveChangesAsync();

            var result = await _repo.GetAllAsync(true);

            Assert.Equal(2, result.Count);
        }

        //GetBySlugAsync

        [Fact]
        public async Task GetBySlugAsync_Should_BeCaseInsensitive()
        {
            _context.Categories.Add(CreateCategory(1, "Books"));
            await _context.SaveChangesAsync();

            var result = await _repo.GetBySlugAsync("BOOKS");

            Assert.NotNull(result);
            Assert.Equal("Books", result!.Name);
        }

        //ExistsByNameAsync

        [Fact]
        public async Task ExistsByNameAsync_Should_ReturnTrue_WhenExists()
        {
            _context.Categories.Add(CreateCategory(1, "Tools"));
            await _context.SaveChangesAsync();

            var result = await _repo.ExistsByNameAsync("tools");

            Assert.True(result);
        }

        //GetItemCountAsync

        [Fact]
        public async Task GetItemCountAsync_Should_CountOnlyActiveApproved()
        {
            _context.Users.Add(CreateUser("u1"));
            _context.Categories.Add(CreateCategory(1, "Cat"));
            await _context.SaveChangesAsync();

            _context.Items.AddRange(
                CreateItem(1, 1, true, ItemStatus.Approved),
                CreateItem(2, 1, false, ItemStatus.Approved),
                CreateItem(3, 1, true, ItemStatus.Pending)
            );
            await _context.SaveChangesAsync();

            var count = await _repo.GetItemCountAsync(1);

            Assert.Equal(1, count);
        }

        //GetByIdWithCountAsync

        [Fact]
        public async Task GetByIdWithCountAsync_Should_ReturnCorrectCount()
        {
            _context.Users.Add(CreateUser("u1"));
            _context.Categories.Add(CreateCategory(1, "Cat"));
            await _context.SaveChangesAsync();

            _context.Items.Add(CreateItem(1, 1));
            await _context.SaveChangesAsync();

            var result = await _repo.GetByIdWithCountAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result!.ItemCount);
        }

        //GetAllWithCountsAsync

        [Fact]
        public async Task GetAllWithCountsAsync_Should_FilterInactive_ForNonAdmin()
        {
            _context.Users.Add(CreateUser("u1"));
            _context.Categories.AddRange(
                CreateCategory(1, "A", true),
                CreateCategory(2, "B", false)
            );
            await _context.SaveChangesAsync();

            _context.Items.Add(CreateItem(1, 1));
            await _context.SaveChangesAsync();

            var result = await _repo.GetAllWithCountsAsync(false);

            Assert.Single(result);
            Assert.Equal("A", result[0].Category.Name);
            Assert.Equal(1, result[0].ItemCount);
        }

        [Fact]
        public async Task GetAllWithCountsAsync_Should_ReturnAll_ForAdmin()
        {
            _context.Categories.AddRange(
                CreateCategory(1, "A", true),
                CreateCategory(2, "B", false)
            );
            await _context.SaveChangesAsync();

            var result = await _repo.GetAllWithCountsAsync(true);

            Assert.Equal(2, result.Count);
        }
    }
}