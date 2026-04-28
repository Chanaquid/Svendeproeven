using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Repositories
{
    public class FineRepositoryTests
    {
        private ApplicationDbContext GetDbContext() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        private FineRepository GetRepository(ApplicationDbContext context) =>
            new FineRepository(context);

        private ApplicationUser MakeUser(string id) => new ApplicationUser
        {
            Id = id,
            UserName = id,
            Email = $"{id}@test.com",
            FullName = "Test User"
        };

        private Item MakeItem(int id, string ownerId) => new Item
        {
            Id = id,
            OwnerId = ownerId,
            Title = "Test Item",
            Description = "Test",
            PricePerDay = 10,
            Condition = ItemCondition.Good,
            Status = ItemStatus.Approved,
            RowVersion = new byte[] { 1 },
            Slug = $"item-{id}",
            QrCode = $"QR{id:D8}",
            IsActive = true,
            Availability = ItemAvailability.Available,
            AvailableFrom = DateTime.UtcNow.AddDays(-1),
            AvailableUntil = DateTime.UtcNow.AddDays(30),
            PickupAddress = "Test Address",
            CurrentValue = 100
        };

        private Loan MakeLoan(int id, string lenderId, string borrowerId, int itemId) => new Loan
        {
            Id = id,
            LenderId = lenderId,
            BorrowerId = borrowerId,
            ItemId = itemId,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(3),
            TotalPrice = 30,
            PricePerDaySnapshot = 10,
            SnapshotCondition = ItemCondition.Good,
            Status = LoanStatus.Active
        };

        [Fact]
        public async Task AddAsync_ShouldAddFine()
        {
            var context = GetDbContext();
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
            var context = GetDbContext();
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
            var context = GetDbContext();
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
            var context = GetDbContext();
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
            var context = GetDbContext();

            context.Users.Add(MakeUser("user1"));
            context.Items.Add(MakeItem(1, "user1"));
            context.Loans.Add(MakeLoan(1, "user1", "user1", 1));
            await context.SaveChangesAsync();

            context.Fines.Add(new Fine
            {
                UserId = "user1",
                LoanId = 1,
                DisputeId = null,
                Amount = 50,
                Type = FineType.Custom,
                Status = FineStatus.Unpaid
            });
            await context.SaveChangesAsync();

            var repo = GetRepository(context);
            var result = await repo.ExistsActiveFineAsync("user1", 1, null);

            Assert.True(result);
        }

        [Fact]
        public async Task ExistsActiveFineAsync_ShouldReturnFalse_WhenFineIsVoided()
        {
            var context = GetDbContext();

            context.Users.Add(MakeUser("user1"));
            context.Items.Add(MakeItem(1, "user1"));
            context.Loans.Add(MakeLoan(1, "user1", "user1", 1));
            await context.SaveChangesAsync();

            context.Fines.Add(new Fine
            {
                UserId = "user1",
                LoanId = 1,
                DisputeId = null,
                Amount = 50,
                Type = FineType.Custom,
                Status = FineStatus.Voided
            });
            await context.SaveChangesAsync();

            var repo = GetRepository(context);
            var result = await repo.ExistsActiveFineAsync("user1", 1, null);

            Assert.False(result);
        }

        [Fact]
        public async Task GetOutstandingTotalByUserAsync_ShouldSumUnpaidAndPendingOnly()
        {
            var context = GetDbContext();
            context.Users.Add(MakeUser("user1"));
            context.Fines.AddRange(
                new Fine { UserId = "user1", Amount = 100, Type = FineType.Custom, Status = FineStatus.Unpaid },
                new Fine { UserId = "user1", Amount = 50, Type = FineType.Custom, Status = FineStatus.PendingVerification },
                new Fine { UserId = "user1", Amount = 200, Type = FineType.Custom, Status = FineStatus.Paid }
            );
            await context.SaveChangesAsync();

            var repo = GetRepository(context);
            var result = await repo.GetOutstandingTotalByUserAsync("user1");

            Assert.Equal(150, result);
        }

        [Fact]
        public async Task GetStatusCountsAsync_ShouldReturnGroupedCounts()
        {
            var context = GetDbContext();
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
            var context = GetDbContext();
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
            var context = GetDbContext();
            context.Users.AddRange(MakeUser("user1"), MakeUser("user2"));
            context.Fines.AddRange(
                new Fine { UserId = "user1", Amount = 100, Type = FineType.Custom, Status = FineStatus.Unpaid },
                new Fine { UserId = "user2", Amount = 200, Type = FineType.Custom, Status = FineStatus.PendingVerification },
                new Fine { UserId = "user1", Amount = 999, Type = FineType.Custom, Status = FineStatus.Voided }
            );
            await context.SaveChangesAsync();

            var repo = GetRepository(context);
            var result = await repo.GetOutstandingTotalAsync();

            Assert.Equal(300, result);
        }

        [Fact]
        public async Task GetByDisputeIdAsync_ShouldReturnFinesForDispute()
        {
            var context = GetDbContext();

            context.Users.Add(MakeUser("user1"));
            context.Items.Add(MakeItem(1, "user1"));
            context.Loans.Add(MakeLoan(1, "user1", "user1", 1));
            await context.SaveChangesAsync();

            context.Disputes.AddRange(
                new Dispute
                {
                    Id = 5,
                    LoanId = 1,
                    FiledById = "user1",
                    Description = "Dispute 5",
                    FiledAs = DisputeFiledAs.AsBorrower,
                    ResponseDeadline = DateTime.UtcNow.AddHours(72)
                },
                new Dispute
                {
                    Id = 99,
                    LoanId = 1,
                    FiledById = "user1",
                    Description = "Dispute 99",
                    FiledAs = DisputeFiledAs.AsBorrower,
                    ResponseDeadline = DateTime.UtcNow.AddHours(72)
                }
            );
            await context.SaveChangesAsync();

            context.Fines.AddRange(
                new Fine { UserId = "user1", Amount = 50, Type = FineType.ResultedByDispute, DisputeId = 5 },
                new Fine { UserId = "user1", Amount = 50, Type = FineType.ResultedByDispute, DisputeId = 99 }
            );
            await context.SaveChangesAsync();

            var repo = GetRepository(context);
            var result = await repo.GetByDisputeIdAsync(5);

            Assert.Single(result);
            Assert.Equal(5, result[0].DisputeId);
        }
    }
}