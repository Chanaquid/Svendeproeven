using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Repositories
{
    public class DisputeRepositoryTests
    {
        private ApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private DisputeRepository GetRepository(ApplicationDbContext context)
        {
            return new DisputeRepository(context);
        }

        private ApplicationUser MakeUser(string id) => new ApplicationUser
        {
            Id = id,
            UserName = id,
            Email = $"{id}@test.com",
            FullName = $"User {id}"
        };

        private Loan MakeLoan(int id, string userId)
        {
            return new Loan
            {
                Id = id,
                LenderId = userId,
                BorrowerId = userId,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(3),
                TotalPrice = 30,
                PricePerDaySnapshot = 10,
                SnapshotCondition = ItemCondition.Good,
                Status = LoanStatus.Active
            };
        }

        [Fact]
        public async Task AddAsync_ShouldAddDispute()
        {
            var context = GetDbContext();
            var repo = GetRepository(context);

            var dispute = new Dispute
            {
                LoanId = 1,
                FiledById = "user1",
                Description = "Test dispute",
                FiledAs = DisputeFiledAs.AsBorrower,
                ResponseDeadline = DateTime.UtcNow.AddHours(72)
            };

            await repo.AddAsync(dispute);
            await repo.SaveChangesAsync();

            var result = await context.Disputes.FirstOrDefaultAsync();

            Assert.NotNull(result);
            Assert.Equal("Test dispute", result!.Description);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnDispute()
        {
            var context = GetDbContext();

            var dispute = new Dispute
            {
                Id = 1,
                LoanId = 1,
                FiledById = "user1",
                Description = "Test",
                FiledAs = DisputeFiledAs.AsBorrower,
                ResponseDeadline = DateTime.UtcNow.AddHours(72)
            };

            context.Disputes.Add(dispute);
            await context.SaveChangesAsync();

            var repo = GetRepository(context);
            var result = await repo.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result!.Id);
        }

        [Fact]
        public async Task GetActiveDisputeByLoanIdAsync_ShouldReturnActiveDispute()
        {
            var context = GetDbContext();

            //Seed the user and loan that the dispute requires
            var user = MakeUser("user1");
            context.Users.Add(user);

            var loan = MakeLoan(10, "user1");
            context.Loans.Add(loan);

            await context.SaveChangesAsync();

            context.Disputes.Add(new Dispute
            {
                LoanId = 10,
                FiledById = "user1",
                Description = "Active",
                Status = DisputeStatus.AwaitingResponse,
                FiledAs = DisputeFiledAs.AsBorrower,
                ResponseDeadline = DateTime.UtcNow.AddHours(72)
            });

            await context.SaveChangesAsync();

            var repo = GetRepository(context);
            var result = await repo.GetActiveDisputeByLoanIdAsync(10);

            Assert.NotNull(result);
            Assert.Equal(10, result!.LoanId);
        }

        [Fact]
        public async Task HasActiveDisputeAsync_ShouldReturnTrue()
        {
            var context = GetDbContext();

            context.Disputes.Add(new Dispute
            {
                LoanId = 99,
                FiledById = "user1",
                Description = "Test",
                Status = DisputeStatus.PendingAdminReview,
                FiledAs = DisputeFiledAs.AsBorrower,
                ResponseDeadline = DateTime.UtcNow.AddHours(72)
            });

            await context.SaveChangesAsync();

            var repo = GetRepository(context);
            var result = await repo.HasActiveDisputeAsync(99);

            Assert.True(result);
        }

        [Fact]
        public async Task GetOpenCountAsync_ShouldReturnCorrectCount()
        {
            var context = GetDbContext();

            context.Disputes.AddRange(
                new Dispute { LoanId = 1, FiledById = "user1", Status = DisputeStatus.AwaitingResponse, FiledAs = DisputeFiledAs.AsBorrower, ResponseDeadline = DateTime.UtcNow.AddHours(72) },
                new Dispute { LoanId = 1, FiledById = "user1", Status = DisputeStatus.PendingAdminReview, FiledAs = DisputeFiledAs.AsBorrower, ResponseDeadline = DateTime.UtcNow.AddHours(72) },
                new Dispute { LoanId = 1, FiledById = "user1", Status = DisputeStatus.Resolved, FiledAs = DisputeFiledAs.AsBorrower, ResponseDeadline = DateTime.UtcNow.AddHours(72) }
            );

            await context.SaveChangesAsync();

            var repo = GetRepository(context);
            var result = await repo.GetOpenCountAsync();

            Assert.Equal(2, result);
        }

        [Fact]
        public async Task GetStatusCountsAsync_ShouldReturnGroupedCounts()
        {
            var context = GetDbContext();

            context.Disputes.AddRange(
                new Dispute { LoanId = 1, FiledById = "user1", Status = DisputeStatus.Resolved, FiledAs = DisputeFiledAs.AsBorrower, ResponseDeadline = DateTime.UtcNow.AddHours(72) },
                new Dispute { LoanId = 1, FiledById = "user1", Status = DisputeStatus.Resolved, FiledAs = DisputeFiledAs.AsBorrower, ResponseDeadline = DateTime.UtcNow.AddHours(72) },
                new Dispute { LoanId = 1, FiledById = "user1", Status = DisputeStatus.PendingAdminReview, FiledAs = DisputeFiledAs.AsBorrower, ResponseDeadline = DateTime.UtcNow.AddHours(72) }
            );

            await context.SaveChangesAsync();

            var repo = GetRepository(context);
            var result = await repo.GetStatusCountsAsync();

            Assert.Equal(2, result[DisputeStatus.Resolved]);
            Assert.Equal(1, result[DisputeStatus.PendingAdminReview]);
        }

        [Fact]
        public async Task DeletePhotoAsync_ShouldRemovePhoto()
        {
            var context = GetDbContext();

            var photo = new DisputePhoto
            {
                DisputeId = 1,
                PhotoUrl = "test.jpg",
                SubmittedById = "user1"
            };

            context.DisputePhotos.Add(photo);
            await context.SaveChangesAsync();

            var repo = GetRepository(context);
            await repo.DeletePhotoAsync(photo);
            await repo.SaveChangesAsync();

            var exists = await context.DisputePhotos.AnyAsync();

            Assert.False(exists);
        }
    }
}