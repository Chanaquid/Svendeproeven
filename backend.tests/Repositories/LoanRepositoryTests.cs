using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Repositories
{
    public class LoanRepositoryTests
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

        private Item MakeItem(int id, string ownerId) => new()
        {
            Id = id,
            RowVersion = new byte[] { 1 },
            OwnerId = ownerId,
            Title = "Test Item",
            Slug = $"item-{id}",
            Description = "desc",
            QrCode = $"QR{id:D8}",
            AvailableFrom = DateTime.UtcNow.AddDays(-1),
            AvailableUntil = DateTime.UtcNow.AddDays(30),
            PricePerDay = 10
        };

        private Loan MakeLoan(int itemId, string lenderId, string borrowerId, LoanStatus status = LoanStatus.Pending) => new()
        {
            ItemId = itemId,
            LenderId = lenderId,
            BorrowerId = borrowerId,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(3),
            TotalPrice = 30,
            PricePerDaySnapshot = 10,
            SnapshotCondition = ItemCondition.Good,
            Status = status
        };

        private async Task<(ApplicationUser lender, ApplicationUser borrower, Item item)> SeedBasicAsync(ApplicationDbContext context)
        {
            var lender = MakeUser("lender1");
            var borrower = MakeUser("borrower1");
            context.Users.AddRange(lender, borrower);
            var item = MakeItem(1, lender.Id);
            context.Items.Add(item);
            await context.SaveChangesAsync();
            return (lender, borrower, item);
        }

        [Fact]
        public async Task AddAsync_ShouldAddLoan()
        {
            var context = CreateContext();
            var repo = new LoanRepository(context);
            var (lender, borrower, item) = await SeedBasicAsync(context);

            await repo.AddAsync(MakeLoan(item.Id, lender.Id, borrower.Id));
            await repo.SaveChangesAsync();

            Assert.Equal(1, await context.Loans.CountAsync());
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnLoan()
        {
            var context = CreateContext();
            var repo = new LoanRepository(context);
            var (lender, borrower, item) = await SeedBasicAsync(context);

            var loan = MakeLoan(item.Id, lender.Id, borrower.Id);
            context.Loans.Add(loan);
            await context.SaveChangesAsync();

            var result = await repo.GetByIdAsync(loan.Id);

            Assert.NotNull(result);
            Assert.Equal(loan.Id, result!.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
        {
            var context = CreateContext();
            var repo = new LoanRepository(context);

            var result = await repo.GetByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetActiveLoanByItemIdAsync_ShouldReturnActiveLoan()
        {
            var context = CreateContext();
            var repo = new LoanRepository(context);
            var (lender, borrower, item) = await SeedBasicAsync(context);

            context.Loans.Add(MakeLoan(item.Id, lender.Id, borrower.Id, LoanStatus.Active));
            await context.SaveChangesAsync();

            var result = await repo.GetActiveLoanByItemIdAsync(item.Id);

            Assert.NotNull(result);
            Assert.Equal(LoanStatus.Active, result!.Status);
        }

        [Fact]
        public async Task GetByBorrowerIdAsync_ShouldReturnLoansForBorrower()
        {
            var context = CreateContext();
            var repo = new LoanRepository(context);
            var (lender, borrower, item) = await SeedBasicAsync(context);

            context.Loans.AddRange(
                MakeLoan(item.Id, lender.Id, borrower.Id),
                MakeLoan(item.Id, lender.Id, borrower.Id)
            );
            await context.SaveChangesAsync();

            var result = await repo.GetByBorrowerIdAsync(borrower.Id);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetByStatusAsync_ShouldReturnOnlyMatchingStatus()
        {
            var context = CreateContext();
            var repo = new LoanRepository(context);
            var (lender, borrower, item) = await SeedBasicAsync(context);

            context.Loans.AddRange(
                MakeLoan(item.Id, lender.Id, borrower.Id, LoanStatus.Active),
                MakeLoan(item.Id, lender.Id, borrower.Id, LoanStatus.Active),
                MakeLoan(item.Id, lender.Id, borrower.Id, LoanStatus.Completed)
            );
            await context.SaveChangesAsync();

            var result = await repo.GetByStatusAsync(LoanStatus.Active);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task HasOngoingLoansAsBorrower_ShouldReturnTrue_WhenActiveLoanExists()
        {
            var context = CreateContext();
            var repo = new LoanRepository(context);
            var (lender, borrower, item) = await SeedBasicAsync(context);

            context.Loans.Add(MakeLoan(item.Id, lender.Id, borrower.Id, LoanStatus.Active));
            await context.SaveChangesAsync();

            var result = await repo.HasOngoingLoansAsBorrower(borrower.Id);

            Assert.True(result);
        }

        [Fact]
        public async Task HasOngoingLoansAsBorrower_ShouldReturnFalse_WhenOnlyCompletedExists()
        {
            var context = CreateContext();
            var repo = new LoanRepository(context);
            var (lender, borrower, item) = await SeedBasicAsync(context);

            context.Loans.Add(MakeLoan(item.Id, lender.Id, borrower.Id, LoanStatus.Completed));
            await context.SaveChangesAsync();

            var result = await repo.HasOngoingLoansAsBorrower(borrower.Id);

            Assert.False(result);
        }

        [Fact]
        public async Task HasOverlappingLoanAsync_ShouldReturnTrue_WhenDatesOverlap()
        {
            var context = CreateContext();
            var repo = new LoanRepository(context);
            var (lender, borrower, item) = await SeedBasicAsync(context);

            var loan = MakeLoan(item.Id, lender.Id, borrower.Id, LoanStatus.Active);
            loan.StartDate = DateTime.UtcNow;
            loan.EndDate = DateTime.UtcNow.AddDays(5);
            context.Loans.Add(loan);
            await context.SaveChangesAsync();

            var result = await repo.HasOverlappingLoanAsync(item.Id,
                DateTime.UtcNow.AddDays(2),
                DateTime.UtcNow.AddDays(7));

            Assert.True(result);
        }

        [Fact]
        public async Task HasOverlappingLoanAsync_ShouldReturnFalse_WhenCancelledLoanExists()
        {
            var context = CreateContext();
            var repo = new LoanRepository(context);
            var (lender, borrower, item) = await SeedBasicAsync(context);

            var loan = MakeLoan(item.Id, lender.Id, borrower.Id, LoanStatus.Cancelled);
            loan.StartDate = DateTime.UtcNow;
            loan.EndDate = DateTime.UtcNow.AddDays(5);
            context.Loans.Add(loan);
            await context.SaveChangesAsync();

            var result = await repo.HasOverlappingLoanAsync(item.Id,
                DateTime.UtcNow.AddDays(2),
                DateTime.UtcNow.AddDays(7));

            Assert.False(result);
        }

        [Fact]
        public async Task GetActiveLoansCountAsync_ShouldReturnCorrectCount()
        {
            var context = CreateContext();
            var repo = new LoanRepository(context);
            var (lender, borrower, item) = await SeedBasicAsync(context);

            context.Loans.AddRange(
                MakeLoan(item.Id, lender.Id, borrower.Id, LoanStatus.Active),
                MakeLoan(item.Id, lender.Id, borrower.Id, LoanStatus.Active),
                MakeLoan(item.Id, lender.Id, borrower.Id, LoanStatus.Completed) // excluded
            );
            await context.SaveChangesAsync();

            var result = await repo.GetActiveLoansCountAsync();

            Assert.Equal(2, result);
        }

        [Fact]
        public async Task GetAllCompletedLoansCountByUserIdAsync_ShouldCountBothRoles()
        {
            var context = CreateContext();
            var repo = new LoanRepository(context);
            var (lender, borrower, item) = await SeedBasicAsync(context);

            context.Loans.AddRange(
                MakeLoan(item.Id, lender.Id, borrower.Id, LoanStatus.Completed), // borrower role
                MakeLoan(item.Id, lender.Id, borrower.Id, LoanStatus.Completed), // borrower role
                MakeLoan(item.Id, lender.Id, borrower.Id, LoanStatus.Active)     // excluded
            );
            await context.SaveChangesAsync();

            var result = await repo.GetAllCompletedLoansCountByUserIdAsync(borrower.Id);

            Assert.Equal(2, result);
        }

        [Fact]
        public async Task GetExpiredPendingLoansAsync_ShouldReturnOldPendingLoans()
        {
            var context = CreateContext();
            var repo = new LoanRepository(context);
            var (lender, borrower, item) = await SeedBasicAsync(context);

            var old = MakeLoan(item.Id, lender.Id, borrower.Id, LoanStatus.Pending);
            old.CreatedAt = DateTime.UtcNow.AddDays(-5);

            var recent = MakeLoan(item.Id, lender.Id, borrower.Id, LoanStatus.Pending);
            recent.CreatedAt = DateTime.UtcNow;

            context.Loans.AddRange(old, recent);
            await context.SaveChangesAsync();

            var cutoff = DateTime.UtcNow.AddDays(-1);
            var result = await repo.GetExpiredPendingLoansAsync(cutoff);

            Assert.Single(result);
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_WhenLoanExists()
        {
            var context = CreateContext();
            var repo = new LoanRepository(context);
            var (lender, borrower, item) = await SeedBasicAsync(context);

            var loan = MakeLoan(item.Id, lender.Id, borrower.Id);
            context.Loans.Add(loan);
            await context.SaveChangesAsync();

            var result = await repo.ExistsAsync(loan.Id);

            Assert.True(result);
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnFalse_WhenLoanDoesNotExist()
        {
            var context = CreateContext();
            var repo = new LoanRepository(context);

            var result = await repo.ExistsAsync(999);

            Assert.False(result);
        }
    }
}