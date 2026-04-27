using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Repositories
{
    public class AdminUserRepositoryTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _context;
        private readonly AdminUserRepository _repo;

        public AdminUserRepositoryTests()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            _repo = new AdminUserRepository(_context);
        }

        public void Dispose()
        {
            _connection.Close();
        }

        //Helpers
        private ApplicationUser CreateUser(string id, bool isDeleted = false, bool isBanned = false)
        {
            return new ApplicationUser
            {
                Id = id,
                UserName = id,
                Email = $"{id}@test.com",
                FullName = $"User {id}",
                IsDeleted = isDeleted,
                IsBanned = isBanned,
                CreatedAt = DateTime.UtcNow,
                Score = 100
            };
        }

        //GetUserByIdAsync
        [Fact]
        public async Task GetUserByIdAsync_Should_IgnoreDeletedUsers()
        {
            var user = CreateUser("u1", isDeleted: true);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _repo.GetUserByIdAsync("u1");

            Assert.Null(result);
        }

        
        //GetUserByIdWithDetailsAsync
        
        [Fact]
        public async Task GetUserByIdWithDetailsAsync_Should_ReturnCountsCorrectly()
        {
            var user = CreateUser("u1");
            _context.Users.Add(user);

            _context.Fines.Add(new Fine { Id = 1, UserId = "u1", Status = FineStatus.Unpaid, Amount = 100 });
            _context.Appeals.Add(new Appeal { Id = 1, UserId = "u1", Status = AppealStatus.Pending });

            await _context.SaveChangesAsync();

            var result = await _repo.GetUserByIdWithDetailsAsync("u1");

            Assert.NotNull(result);
            Assert.Equal(1, result!.FinesCount);
            Assert.Equal(1, result.AppealsCount);
        }

        //GetAllBannedUsersAsync
        [Fact]
        public async Task GetAllBannedUsersAsync_Should_FilterAndReturnPaged()
        {
            var u1 = CreateUser("u1", isBanned: true);
            var u2 = CreateUser("u2", isBanned: false);

            _context.Users.AddRange(u1, u2);
            await _context.SaveChangesAsync();

            var request = new PagedRequest { Page = 1, PageSize = 10 };

            var result = await _repo.GetAllBannedUsersAsync(null, request);

            Assert.Single(result.Items);
            Assert.Equal("u1", result.Items[0].User.Id);
        }

        //GetUsersAsync (search + filtering)

        [Fact]
        public async Task GetUsersAsync_Should_FilterBySearch()
        {
            var u1 = CreateUser("u1");
            u1.FullName = "Alice";

            var u2 = CreateUser("u2");
            u2.FullName = "Bob";

            _context.Users.AddRange(u1, u2);
            await _context.SaveChangesAsync();

            var filter = new UserFilter { Search = "Ali" };
            var request = new PagedRequest { Page = 1, PageSize = 10 };

            var result = await _repo.GetUsersAsync(filter, request);

            Assert.Single(result.Items);
            Assert.Equal("Alice", result.Items[0].User.FullName);
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_Should_Commit_When_NoException()
        {
            await _repo.ExecuteInTransactionAsync(async () =>
            {
                _context.Users.Add(CreateUser("u1"));
                await Task.CompletedTask;
            });

            Assert.Equal(1, await _context.Users.CountAsync());
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_Should_Rollback_OnException()
        {
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await _repo.ExecuteInTransactionAsync(async () =>
                {
                    _context.Users.Add(CreateUser("u1"));
                    throw new Exception("fail");
                });
            });

            Assert.Equal(0, await _context.Users.CountAsync());
        }
    }
}