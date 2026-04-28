using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace backend.Tests.Services
{
    public class ReportServiceTests
    {
        private readonly Mock<IReportRepository> _reportRepositoryMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IItemRepository> _itemRepositoryMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();

        private ReportService CreateService()
        {
            return new ReportService(
                _reportRepositoryMock.Object,
                _userRepositoryMock.Object,
                _itemRepositoryMock.Object,
                _notificationServiceMock.Object
            );
        }

        private static ApplicationUser CreateUser(string id = "user-123")
        {
            return new ApplicationUser
            {
                Id = id,
                FullName = "Test User",
                UserName = "testuser",
                AvatarUrl = "avatar.png"
            };
        }

        private static Report CreateReport(
            int id = 1,
            string reportedById = "user-123",
            ReportStatus status = ReportStatus.Pending)
        {
            var user = CreateUser(reportedById);

            return new Report
            {
                Id = id,
                ReportedById = reportedById,
                ReportedBy = user,
                Type = ReportType.User,
                TargetId = "target-user-123",
                Reasons = ReportReason.Scammer,
                AdditionalDetails = "Some details",
                Status = status,
                CreatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task CreateReportAsync_ShouldThrow_WhenUserDoesNotExist()
        {
            var service = CreateService();

            var dto = new CreateReportDto
            {
                Type = ReportType.User,
                TargetId = "target-user-123",
                Reasons = ReportReason.Scammer,
                AdditionalDetails = "Some details"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync("missing-user"))
                .ReturnsAsync((ApplicationUser?)null);

            Func<Task> act = async () => await service.CreateReportAsync("missing-user", dto);

            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("User not found.");

            _reportRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Report>()), Times.Never);
        }

        [Fact]
        public async Task CreateReportAsync_ShouldThrow_WhenUserReportsThemself()
        {
            var service = CreateService();
            var user = CreateUser();

            var dto = new CreateReportDto
            {
                Type = ReportType.User,
                TargetId = user.Id,
                Reasons = ReportReason.Scammer,
                AdditionalDetails = "Some details"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            Func<Task> act = async () => await service.CreateReportAsync(user.Id, dto);

            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("You cannot report yourself.");

            _reportRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Report>()), Times.Never);
        }

        [Fact]
        public async Task CreateReportAsync_ShouldThrow_WhenUserIsOnCooldown()
        {
            var service = CreateService();
            var user = CreateUser();

            var dto = new CreateReportDto
            {
                Type = ReportType.User,
                TargetId = "target-user-123",
                Reasons = ReportReason.Scammer,
                AdditionalDetails = "Some details"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _reportRepositoryMock
                .Setup(r => r.GetLastReportTimeByUserAsync(user.Id))
                .ReturnsAsync(DateTime.UtcNow.AddMinutes(-10));

            Func<Task> act = async () => await service.CreateReportAsync(user.Id, dto);

            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("You must wait * minute(s) before submitting another report.");

            _reportRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Report>()), Times.Never);
        }

        [Fact]
        public async Task CreateReportAsync_ShouldThrow_WhenTargetAlreadyReported()
        {
            var service = CreateService();
            var user = CreateUser();

            var dto = new CreateReportDto
            {
                Type = ReportType.User,
                TargetId = "target-user-123",
                Reasons = ReportReason.Scammer,
                AdditionalDetails = "Some details"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _reportRepositoryMock
                .Setup(r => r.GetLastReportTimeByUserAsync(user.Id))
                .ReturnsAsync((DateTime?)null);

            _reportRepositoryMock
                .Setup(r => r.HasReportedTargetAsync(user.Id, dto.TargetId, dto.Type))
                .ReturnsAsync(true);

            Func<Task> act = async () => await service.CreateReportAsync(user.Id, dto);

            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("You have already reported this.");

            _reportRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Report>()), Times.Never);
        }

        [Fact]
        public async Task CreateReportAsync_ShouldCreateReport_WhenValid()
        {
            var service = CreateService();
            var user = CreateUser();

            var dto = new CreateReportDto
            {
                Type = ReportType.User,
                TargetId = "target-user-123",
                Reasons = ReportReason.Scammer,
                AdditionalDetails = " Some details "
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _reportRepositoryMock
                .Setup(r => r.GetLastReportTimeByUserAsync(user.Id))
                .ReturnsAsync((DateTime?)null);

            _reportRepositoryMock
                .Setup(r => r.HasReportedTargetAsync(user.Id, dto.TargetId, dto.Type))
                .ReturnsAsync(false);

            _reportRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Report>()))
                .Callback<Report>(report =>
                {
                    report.Id = 1;
                    report.ReportedBy = user;
                })
                .Returns(Task.CompletedTask);

            _reportRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _reportRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(1))
                .ReturnsAsync(CreateReport(1, user.Id));

            var result = await service.CreateReportAsync(user.Id, dto);

            result.Id.Should().Be(1);
            result.ReportedById.Should().Be(user.Id);
            result.IsMine.Should().BeTrue();
            result.Status.Should().Be(ReportStatus.Pending);

            _reportRepositoryMock.Verify(r => r.AddAsync(It.Is<Report>(report =>
                report.ReportedById == user.Id &&
                report.Type == dto.Type &&
                report.TargetId == dto.TargetId &&
                report.Reasons == dto.Reasons &&
                report.AdditionalDetails == "Some details" &&
                report.Status == ReportStatus.Pending
            )), Times.Once);

            _reportRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrow_WhenReportDoesNotExist()
        {
            var service = CreateService();

            _reportRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(99))
                .ReturnsAsync((Report?)null);

            Func<Task> act = async () => await service.GetByIdAsync(99, "user-123", isAdmin: false);

            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Report 99 not found.");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrow_WhenUserDoesNotOwnReport()
        {
            var service = CreateService();
            var report = CreateReport(reportedById: "owner-user");

            _reportRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(report.Id))
                .ReturnsAsync(report);

            Func<Task> act = async () => await service.GetByIdAsync(report.Id, "another-user", isAdmin: false);

            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You cannot view this report.");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnReport_WhenUserOwnsReport()
        {
            var service = CreateService();
            var report = CreateReport();

            _reportRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(report.Id))
                .ReturnsAsync(report);

            var result = await service.GetByIdAsync(report.Id, report.ReportedById, isAdmin: false);

            result.Id.Should().Be(report.Id);
            result.ReportedById.Should().Be(report.ReportedById);
            result.IsMine.Should().BeTrue();
        }

        [Fact]
        public async Task ResolveReportAsync_ShouldThrow_WhenAdminDoesNotExist()
        {
            var service = CreateService();
            var report = CreateReport();

            var dto = new AdminResolveReportDto
            {
                Status = ReportStatus.Resolved,
                AdminNote = "Handled"
            };

            _reportRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(report.Id))
                .ReturnsAsync(report);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync("missing-admin"))
                .ReturnsAsync((ApplicationUser?)null);

            Func<Task> act = async () => await service.ResolveReportAsync(report.Id, "missing-admin", dto);

            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Admin not found.");
        }

        [Fact]
        public async Task ResolveReportAsync_ShouldThrow_WhenAlreadyResolved()
        {
            var service = CreateService();
            var report = CreateReport(status: ReportStatus.Resolved);
            var admin = CreateUser("admin-123");

            var dto = new AdminResolveReportDto
            {
                Status = ReportStatus.Resolved,
                AdminNote = "Handled"
            };

            _reportRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(report.Id))
                .ReturnsAsync(report);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(admin.Id))
                .ReturnsAsync(admin);

            Func<Task> act = async () => await service.ResolveReportAsync(report.Id, admin.Id, dto);

            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("This report has already been resolved.");
        }

        [Fact]
        public async Task ResolveReportAsync_ShouldResolveReport_WhenValid()
        {
            var service = CreateService();
            var report = CreateReport();
            var admin = CreateUser("admin-123");

            var dto = new AdminResolveReportDto
            {
                Status = ReportStatus.Resolved,
                AdminNote = " Report handled "
            };

            _reportRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(report.Id))
                .ReturnsAsync(report);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(admin.Id))
                .ReturnsAsync(admin);

            _reportRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            var result = await service.ResolveReportAsync(report.Id, admin.Id, dto);

            report.Status.Should().Be(ReportStatus.Resolved);
            report.HandledByAdminId.Should().Be(admin.Id);
            report.HandledByAdmin.Should().Be(admin);
            report.AdminNote.Should().Be("Report handled");
            report.ResolvedAt.Should().NotBeNull();

            result.Status.Should().Be(ReportStatus.Resolved);
            result.AdminNote.Should().Be("Report handled");

            _reportRepositoryMock.Verify(r => r.Update(report), Times.Once);
            _reportRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetMyReportsAsync_ShouldReturnPagedReports()
        {
            var service = CreateService();
            var userId = "user-123";

            var request = new PagedRequest { Page = 1, PageSize = 10 };
            var report = CreateReport(reportedById: userId);

            _reportRepositoryMock
                .Setup(r => r.GetByUserIdAsync(userId, null, request))
                .ReturnsAsync(new PagedResult<Report>
                {
                    Items = new List<Report> { report },
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 10
                });

            var result = await service.GetMyReportsAsync(userId, null, request);

            result.Items.Should().HaveCount(1);
            result.Items[0].Id.Should().Be(report.Id);
            result.Items[0].IsMine.Should().BeTrue();
            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPagedReports()
        {
            var service = CreateService();
            var request = new PagedRequest { Page = 1, PageSize = 10 };
            var report = CreateReport();

            _reportRepositoryMock
                .Setup(r => r.GetAllAsync(null, request))
                .ReturnsAsync(new PagedResult<Report>
                {
                    Items = new List<Report> { report },
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 10
                });

            var result = await service.GetAllAsync(null, request);

            result.Items.Should().HaveCount(1);
            result.Items[0].Id.Should().Be(report.Id);
            result.TotalCount.Should().Be(1);
        }
    }
}