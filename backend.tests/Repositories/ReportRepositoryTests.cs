using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests.Repositories
{
    public class ReportRepositoryTests
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

        private Report MakeReport(string reportedById, ReportStatus status = ReportStatus.Pending) => new()
        {
            ReportedById = reportedById,
            Type = ReportType.User,
            TargetId = "target1",
            Reasons = ReportReason.Spam,
            Status = status
        };

        [Fact]
        public async Task AddAsync_ShouldAddReport()
        {
            var context = CreateContext();
            var repo = new ReportRepository(context);

            context.Users.Add(MakeUser("user1"));
            await context.SaveChangesAsync();

            await repo.AddAsync(MakeReport("user1"));
            await repo.SaveChangesAsync();

            Assert.Equal(1, await context.Reports.CountAsync());
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnReport()
        {
            var context = CreateContext();
            var repo = new ReportRepository(context);

            context.Users.Add(MakeUser("user1"));
            var report = MakeReport("user1");
            context.Reports.Add(report);
            await context.SaveChangesAsync();

            var result = await repo.GetByIdAsync(report.Id);

            Assert.NotNull(result);
            Assert.Equal(report.Id, result!.Id);
        }

        [Fact]
        public async Task HasReportedTargetAsync_ShouldReturnTrue_WhenAlreadyReported()
        {
            var context = CreateContext();
            var repo = new ReportRepository(context);

            context.Users.Add(MakeUser("user1"));
            context.Reports.Add(new Report
            {
                ReportedById = "user1",
                Type = ReportType.User,
                TargetId = "target1",
                Reasons = ReportReason.Spam
            });
            await context.SaveChangesAsync();

            var result = await repo.HasReportedTargetAsync("user1", "target1", ReportType.User);

            Assert.True(result);
        }

        [Fact]
        public async Task HasReportedTargetAsync_ShouldReturnFalse_WhenNotReported()
        {
            var context = CreateContext();
            var repo = new ReportRepository(context);

            var result = await repo.HasReportedTargetAsync("user1", "target1", ReportType.User);

            Assert.False(result);
        }

        [Fact]
        public async Task GetLastReportTimeByUserAsync_ShouldReturnMostRecentCreatedAt()
        {
            var context = CreateContext();
            var repo = new ReportRepository(context);

            context.Users.Add(MakeUser("user1"));

            var old = MakeReport("user1");
            old.CreatedAt = DateTime.UtcNow.AddDays(-5);

            var recent = MakeReport("user1");
            recent.CreatedAt = DateTime.UtcNow;

            context.Reports.AddRange(old, recent);
            await context.SaveChangesAsync();

            var result = await repo.GetLastReportTimeByUserAsync("user1");

            Assert.NotNull(result);
            Assert.True(result!.Value >= recent.CreatedAt.AddSeconds(-1));
        }
    }
}