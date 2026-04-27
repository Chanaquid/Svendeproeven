using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Repositories
{
    public class FineRepositoryTests
    {
        private SqliteConnection CreateConnection()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            return connection;
        }

        private ApplicationDbContext GetDbContext(SqliteConnection connection)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private FineRepository GetRepository(ApplicationDbContext context)
        {
            return new FineRepository(context);
        }

        private ApplicationUser MakeUser(string id) => new ApplicationUser
        {
            Id = id,
            UserName = id,
            Email = $"{id}@test.com",
            FullName = "Test User"
        };

        [Fact]
        public async Task AddAsync_ShouldAddFine()
        {
            using var connection = CreateConnection();
            var context = GetDbContext(connection);
            context.Users.Add(MakeUser("user1"));
            await context.SaveChangesAsync();

            var repo = GetRepository(context);
            await repo.AddAsync(new Fine
            {
                UserId = "user1",
                Amount = 50,
                Type = FineType.Custom,
                Status = FineStatus.Unpaid
            });
            await repo.SaveChangesAsync();

            var result = await context.Fines.FirstOrDefaultAsync();

            Assert.NotNull(result);
            Assert.Equal(50, result!.Amount);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnFine()
        {
            using var connection = CreateConnection();
            var context = GetDbContext(connection);
            context.Users.Add(MakeUser("user1"));
            context.Fines.Add(new Fine { Id = 1, UserId = "user1", Amount = 100, Type = FineType.Custom });
            await context.SaveChangesAsync();

            var repo = GetRepository(context);
            var result = await repo.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result!.Id);
        }

        [Fact]
        public async Task HasOutstandingFinesAsync_ShouldReturnTrue_WhenUnpaidExists()
        {
            using var connection = CreateConnection();
            var context = GetDbContext(connection);
            context.Users.Add(MakeUser("user1"));
            context.Fines.Add(new Fine { UserId = "user1", Amount = 75, Type = FineType.Custom, Status = FineStatus.Unpaid });
            await context.SaveChangesAsync();

            var repo = GetRepository(context);
            var result = await repo.HasOutstandingFinesAsync("user1");

            Assert.True(result);
        }

        [Fact]
        public async Task HasOutstandingFinesAsync_ShouldReturnFalse_WhenOnlyPaidExists()
        {
            using var connection = CreateConnection();
            var context = GetDbContext(connection);
            context.Users.Add(MakeUser("user1"));
            context.Fines.Add(new Fine { UserId = "user1", Amount = 75, Type = FineType.Custom, Status = FineStatus.Paid });
            await context.SaveChangesAsync();

            var repo = GetRepository(context);
            var result = await repo.HasOutstandingFinesAsync("user1");

            Assert.False(result);
        }

        [Fact]
        public async Task ExistsActiveFineAsync_ShouldReturnTrue_WhenUnpaidFineExists()
        {
            using var connection = CreateConnection();
            var context = GetDbContext(connection);
            context.Users.Add(MakeUser("user1"));
            context.Fines.Add(new Fine { UserId = "user1", LoanId = 1, DisputeId = null, Amount = 50, Type = FineType.Custom, Status = FineStatus.Unpaid });
            await context.SaveChangesAsync();

            var repo = GetRepository(context);
            var result = await repo.ExistsActiveFineAsync("user1", 1, null);

            Assert.True(result);
        }

        [Fact]
        public async Task ExistsActiveFineAsync_ShouldReturnFalse_WhenFineIsVoided()
        {
            using var connection = CreateConnection();
            var context = GetDbContext(connection);
            context.Users.Add(MakeUser("user1"));
            context.Fines.Add(new Fine { UserId = "user1", LoanId = 1, DisputeId = null, Amount = 50, Type = FineType.Custom, Status = FineStatus.Voided });
            await context.SaveChangesAsync();

            var repo = GetRepository(context);
            var result = await repo.ExistsActiveFineAsync("user1", 1, null);

            Assert.False(result);
        }

        [Fact]
        public async Task GetOutstandingTotalByUserAsync_ShouldSumUnpaidAndPendingOnly()
        {
            using var connection = CreateConnection();
            var context = GetDbContext(connection);
            context.Users.Add(MakeUser("user1"));
            context.Fines.AddRange(
                new Fine { UserId = "user1", Amount = 100, Type = FineType.Custom, Status = FineStatus.Unpaid },
                new Fine { UserId = "user1", Amount = 50, Type = FineType.Custom, Status = FineStatus.PendingVerification },
                new Fine { UserId = "user1", Amount = 200, Type = FineType.Custom, Status = FineStatus.Paid } // excluded
            );
            await context.SaveChangesAsync();

            var repo = GetRepository(context);
            var result = await repo.GetOutstandingTotalByUserAsync("user1");

            Assert.Equal(150, result);
        }

        [Fact]
        public async Task GetStatusCountsAsync_ShouldReturnGroupedCounts()
        {
            using var connection = CreateConnection();
            var context = GetDbContext(connection);
            context.Users.Add(MakeUser("user1"));
            context.Fines.AddRange(
                new Fine { UserId = "user1", Amount = 10, Type = FineType.Custom, Status = FineStatus.Unpaid },
                new Fine { UserId = "user1", Amount = 10, Type = FineType.Custom, Status = FineStatus.Unpaid },
                new Fine { UserId = "user1", Amount = 10, Type = FineType.Custom, Status = FineStatus.Paid }
            );
            await context.SaveChangesAsync();

            var repo = GetRepository(context);
            var result = await repo.GetStatusCountsAsync();

            Assert.Equal(2, result[FineStatus.Unpaid]);
            Assert.Equal(1, result[FineStatus.Paid]);
        }

        [Fact]
        public async Task GetPendingProofCountAsync_ShouldReturnCorrectCount()
        {
            using var connection = CreateConnection();
            var context = GetDbContext(connection);
            context.Users.Add(MakeUser("user1"));
            context.Fines.AddRange(
                new Fine { UserId = "user1", Amount = 10, Type = FineType.Custom, Status = FineStatus.PendingVerification },
                new Fine { UserId = "user1", Amount = 10, Type = FineType.Custom, Status = FineStatus.PendingVerification },
                new Fine { UserId = "user1", Amount = 10, Type = FineType.Custom, Status = FineStatus.Unpaid }
            );
            await context.SaveChangesAsync();

            var repo = GetRepository(context);
            var result = await repo.GetPendingProofCountAsync();

            Assert.Equal(2, result);
        }

        [Fact]
        public async Task GetOutstandingTotalAsync_ShouldSumAcrossAllUsers()
        {
            using var connection = CreateConnection();
            var context = GetDbContext(connection);
            context.Users.AddRange(MakeUser("user1"), MakeUser("user2"));
            context.Fines.AddRange(
                new Fine { UserId = "user1", Amount = 100, Type = FineType.Custom, Status = FineStatus.Unpaid },
                new Fine { UserId = "user2", Amount = 200, Type = FineType.Custom, Status = FineStatus.PendingVerification },
                new Fine { UserId = "user1", Amount = 999, Type = FineType.Custom, Status = FineStatus.Voided } // excluded
            );
            await context.SaveChangesAsync();

            var repo = GetRepository(context);
            var result = await repo.GetOutstandingTotalAsync();

            Assert.Equal(300, result);
        }

        [Fact]
        public async Task GetByDisputeIdAsync_ShouldReturnFinesForDispute()
        {
            using var connection = CreateConnection();
            var context = GetDbContext(connection);
            context.Users.Add(MakeUser("user1"));
            context.Fines.AddRange(
                new Fine { UserId = "user1", Amount = 50, Type = FineType.ResultedByDispute, DisputeId = 5 },
                new Fine { UserId = "user1", Amount = 50, Type = FineType.ResultedByDispute, DisputeId = 99 } // different dispute
            );
            await context.SaveChangesAsync();

            var repo = GetRepository(context);
            var result = await repo.GetByDisputeIdAsync(5);

            Assert.Single(result);
            Assert.Equal(5, result[0].DisputeId);
        }
    }
}