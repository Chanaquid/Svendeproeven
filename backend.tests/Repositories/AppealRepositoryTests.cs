using backend.Data;
using backend.Dtos;
using backend.Models;
using backend.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Repositories
{
    public class AppealRepositoryTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _context;
        private readonly AppealRepository _repo;

        public AppealRepositoryTests()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            _repo = new AppealRepository(_context);
        }

        public void Dispose()
        {
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

        private Appeal CreateAppeal(int id, string userId, AppealStatus status, AppealType type)
        {
            return new Appeal
            {
                Id = id,
                UserId = userId,
                Status = status,
                AppealType = type,
                Message = "Test message",
                CreatedAt = DateTime.UtcNow
            };
        }

        //GetByIdWithDetailsAsync
        [Fact]
        public async Task GetByIdWithDetailsAsync_Should_Include_RelatedEntities()
        {
            var user = CreateUser("u1");
            _context.Users.Add(user);

            var fine = new Fine { Id = 1, UserId = "u1", Amount = 100 };
            _context.Fines.Add(fine);

            var appeal = CreateAppeal(1, "u1", AppealStatus.Pending, AppealType.Fine);
            appeal.FineId = fine.Id;

            _context.Appeals.Add(appeal);
            await _context.SaveChangesAsync();

            var result = await _repo.GetByIdWithDetailsAsync(1);

            Assert.NotNull(result);
            Assert.NotNull(result!.User);
            Assert.NotNull(result.Fine);
        }



        [Fact]
        public async Task GetPendingByUserIdAsync_Should_Return_OnlyPendingForUser()
        {
            _context.Users.AddRange(CreateUser("u1"), CreateUser("u2")); //add u2
            _context.Appeals.AddRange(
                CreateAppeal(1, "u1", AppealStatus.Pending, AppealType.Fine),
                CreateAppeal(2, "u1", AppealStatus.Approved, AppealType.Fine),
                CreateAppeal(3, "u2", AppealStatus.Pending, AppealType.Fine)
            );
            await _context.SaveChangesAsync();

            var request = new PagedRequest { Page = 1, PageSize = 10 };
            var result = await _repo.GetPendingByUserIdAsync("u1", null, request);

            Assert.Single(result.Items);
            Assert.Equal(1, result.Items[0].Id);
        }


        //HasPendingScoreAppealAsync
        [Fact]
        public async Task HasPendingScoreAppealAsync_Should_ReturnTrue_WhenExists()
        {
            _context.Users.Add(CreateUser("u1"));

            _context.Appeals.Add(
                CreateAppeal(1, "u1", AppealStatus.Pending, AppealType.Score)
            );

            await _context.SaveChangesAsync();

            var result = await _repo.HasPendingScoreAppealAsync("u1");

            Assert.True(result);
        }

        //GetPendingFineAppealByFineIdAsync
        [Fact]
        public async Task GetPendingFineAppealByFineIdAsync_Should_ReturnCorrectAppeal()
        {
            _context.Users.Add(CreateUser("u1"));
            _context.Fines.Add(new Fine { Id = 10, UserId = "u1", Amount = 100 }); // satisfy FK
            await _context.SaveChangesAsync();

            var appeal = CreateAppeal(1, "u1", AppealStatus.Pending, AppealType.Fine);
            appeal.FineId = 10;
            _context.Appeals.Add(appeal);
            await _context.SaveChangesAsync();

            var result = await _repo.GetPendingFineAppealByFineIdAsync(10);

            Assert.NotNull(result);
            Assert.Equal(1, result!.Id);
        }

        //GetAllAsync (search + filtering)
        [Fact]
        public async Task GetAllAsync_Should_Filter_By_Search()
        {
            var user = CreateUser("u1");
            user.FullName = "Alice";
            _context.Users.Add(user);

            _context.Appeals.AddRange(
                new Appeal
                {
                    Id = 1,
                    UserId = "u1",
                    Message = "Problem with item",
                    Status = AppealStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                },
                new Appeal
                {
                    Id = 2,
                    UserId = "u1",
                    Message = "Different issue",
                    Status = AppealStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                }
            );

            await _context.SaveChangesAsync();

            var filter = new AppealFilter { Search = "problem" };
            var request = new PagedRequest { Page = 1, PageSize = 10 };

            var result = await _repo.GetAllAsync(filter, request);

            Assert.Single(result.Items);
            Assert.Equal(1, result.Items[0].Id);
        }
    }
}