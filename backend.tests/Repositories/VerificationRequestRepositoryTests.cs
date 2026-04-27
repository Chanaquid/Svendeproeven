using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Repositories
{
    public class VerificationRequestRepositoryTests
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

        private VerificationRequest MakeRequest(string userId, VerificationStatus status = VerificationStatus.Pending) => new()
        {
            UserId = userId,
            DocumentUrl = "https://example.com/doc.jpg",
            DocumentType = VerificationDocumentType.Passport,
            Status = status
        };

        [Fact]
        public async Task AddAsync_ShouldAddRequest()
        {
            var context = CreateContext();
            var repo = new VerificationRequestRepository(context);

            context.Users.Add(MakeUser("user1"));
            await context.SaveChangesAsync();

            await repo.AddAsync(MakeRequest("user1"));
            await repo.SaveChangesAsync();

            Assert.Equal(1, await context.VerificationRequests.CountAsync());
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnRequest()
        {
            var context = CreateContext();
            var repo = new VerificationRequestRepository(context);

            context.Users.Add(MakeUser("user1"));
            var req = MakeRequest("user1");
            context.VerificationRequests.Add(req);
            await context.SaveChangesAsync();

            var result = await repo.GetByIdAsync(req.Id);

            Assert.NotNull(result);
            Assert.Equal(req.Id, result!.Id);
        }

        [Fact]
        public async Task HasPendingRequestAsync_ShouldReturnTrue_WhenPendingExists()
        {
            var context = CreateContext();
            var repo = new VerificationRequestRepository(context);

            context.Users.Add(MakeUser("user1"));
            context.VerificationRequests.Add(MakeRequest("user1", VerificationStatus.Pending));
            await context.SaveChangesAsync();

            var result = await repo.HasPendingRequestAsync("user1");

            Assert.True(result);
        }

        [Fact]
        public async Task HasPendingRequestAsync_ShouldReturnFalse_WhenOnlyApprovedExists()
        {
            var context = CreateContext();
            var repo = new VerificationRequestRepository(context);

            context.Users.Add(MakeUser("user1"));
            context.VerificationRequests.Add(MakeRequest("user1", VerificationStatus.Approved));
            await context.SaveChangesAsync();

            var result = await repo.HasPendingRequestAsync("user1");

            Assert.False(result);
        }

        [Fact]
        public async Task GetPendingByUserIdAsync_ShouldReturnPendingRequest()
        {
            var context = CreateContext();
            var repo = new VerificationRequestRepository(context);

            context.Users.Add(MakeUser("user1"));
            context.VerificationRequests.AddRange(
                MakeRequest("user1", VerificationStatus.Pending),
                MakeRequest("user1", VerificationStatus.Rejected) //excluded
            );
            await context.SaveChangesAsync();

            var result = await repo.GetPendingByUserIdAsync("user1");

            Assert.NotNull(result);
            Assert.Equal(VerificationStatus.Pending, result!.Status);
        }
    }
}