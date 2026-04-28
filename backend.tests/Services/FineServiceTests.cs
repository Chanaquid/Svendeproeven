using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace backend.Tests.Services
{
    public class FineServiceTests
    {
        private readonly Mock<IFineRepository> _fineRepositoryMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly Mock<ILoanRepository> _loanRepositoryMock = new();
        private readonly Mock<IDisputeRepository> _disputeRepositoryMock = new();
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

        public FineServiceTests()
        {
            _userManagerMock = MockUserManager();
        }

        private FineService CreateService()
        {
            return new FineService(
                _fineRepositoryMock.Object,
                _notificationServiceMock.Object,
                _loanRepositoryMock.Object,
                _disputeRepositoryMock.Object,
                _userManagerMock.Object
            );
        }

        private static Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();

            return new Mock<UserManager<ApplicationUser>>(
                store.Object,
                Mock.Of<IOptions<IdentityOptions>>(),
                Mock.Of<IPasswordHasher<ApplicationUser>>(),
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                Mock.Of<ILookupNormalizer>(),
                new IdentityErrorDescriber(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<ApplicationUser>>>()
            );
        }

        private static ApplicationUser CreateUser()
        {
            return new ApplicationUser
            {
                Id = "user-123",
                FullName = "Test User",
                UserName = "testuser",
                AvatarUrl = "avatar.png"
            };
        }

        private static Fine CreateFine(
            int id = 1,
            string userId = "user-123",
            FineStatus status = FineStatus.Unpaid)
        {
            var user = CreateUser();

            return new Fine
            {
                Id = id,
                UserId = userId,
                User = user,
                Amount = 500,
                Type = FineType.Custom,
                Status = status,
                AdminNote = "Test fine",
                CreatedAt = DateTime.UtcNow,
                Loan = new Loan
                {
                    Id = 10,
                    Item = new Item
                    {
                        Id = 99,
                        Title = "Test Item",
                        Slug = "test-item"
                    }
                }
            };
        }

        [Fact]
        public async Task CreateCustomFineAsync_ShouldThrow_WhenUserDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var dto = new CreateCustomFineDto
            {
                UserId = "missing-user",
                Amount = 100,
                Reason = "Rule violation"
            };

            _userManagerMock
                .Setup(m => m.FindByIdAsync(dto.UserId))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            Func<Task> act = async () => await service.CreateCustomFineAsync("admin-123", dto);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("User not found.");
        }

        [Fact]
        public async Task CreateCustomFineAsync_ShouldThrow_WhenAmountIsZeroOrLess()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser();

            var dto = new CreateCustomFineDto
            {
                UserId = user.Id,
                Amount = 0,
                Reason = "Rule violation"
            };

            _userManagerMock
                .Setup(m => m.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            Func<Task> act = async () => await service.CreateCustomFineAsync("admin-123", dto);

            // Assert
            await act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("Fine amount must be greater than zero.");

            _fineRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Fine>()), Times.Never);
        }

        [Fact]
        public async Task CreateCustomFineAsync_ShouldThrow_WhenReasonIsEmpty()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser();

            var dto = new CreateCustomFineDto
            {
                UserId = user.Id,
                Amount = 100,
                Reason = "   "
            };

            _userManagerMock
                .Setup(m => m.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            Func<Task> act = async () => await service.CreateCustomFineAsync("admin-123", dto);

            // Assert
            await act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("A reason is required for custom fines.");

            _fineRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Fine>()), Times.Never);
        }

        [Fact]
        public async Task CreateCustomFineAsync_ShouldCreateFine_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser();

            var dto = new CreateCustomFineDto
            {
                UserId = user.Id,
                Amount = 250,
                Reason = "Late return"
            };

            _userManagerMock
                .Setup(m => m.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            _fineRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Fine>()))
                .Callback<Fine>(fine =>
                {
                    fine.Id = 1;
                    fine.User = user;
                })
                .Returns(Task.CompletedTask);

            _fineRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _notificationServiceMock
                .Setup(n => n.SendAsync(
                    It.IsAny<string>(),
                    It.IsAny<NotificationType>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<NotificationReferenceType?>()))
                .Returns(Task.CompletedTask);

            _fineRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(1))
                .ReturnsAsync(CreateFine(id: 1, userId: user.Id));

            // Act
            var result = await service.CreateCustomFineAsync("admin-123", dto);

            // Assert
            result.Id.Should().Be(1);
            result.UserId.Should().Be(user.Id);

            _fineRepositoryMock.Verify(r => r.AddAsync(It.Is<Fine>(f =>
                f.UserId == user.Id &&
                f.Amount == dto.Amount &&
                f.Type == FineType.Custom &&
                f.Status == FineStatus.Unpaid &&
                f.AdminNote == dto.Reason &&
                f.IssuedByAdminId == "admin-123"
            )), Times.Once);

            _fineRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateFineAsync_ShouldThrow_WhenFineDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var dto = new UpdateFineDto
            {
                FineId = 1,
                Amount = 300,
                Reason = "Updated reason"
            };

            _fineRepositoryMock
                .Setup(r => r.GetByIdAsync(dto.FineId))
                .ReturnsAsync((Fine?)null);

            // Act
            Func<Task> act = async () => await service.UpdateFineAsync("admin-123", dto);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Fine not found.");
        }

        [Fact]
        public async Task UpdateFineAsync_ShouldThrow_WhenFineIsPaid()
        {
            // Arrange
            var service = CreateService();

            var fine = CreateFine(status: FineStatus.Paid);

            var dto = new UpdateFineDto
            {
                FineId = fine.Id,
                Amount = 300
            };

            _fineRepositoryMock
                .Setup(r => r.GetByIdAsync(fine.Id))
                .ReturnsAsync(fine);

            // Act
            Func<Task> act = async () => await service.UpdateFineAsync("admin-123", dto);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Cannot update a paid fine.");
        }

        [Fact]
        public async Task VoidFineAsync_ShouldVoidFine_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var fine = CreateFine(status: FineStatus.Unpaid);

            _fineRepositoryMock
                .Setup(r => r.GetByIdAsync(fine.Id))
                .ReturnsAsync(fine);

            _fineRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _notificationServiceMock
                .Setup(n => n.SendAsync(
                    It.IsAny<string>(),
                    It.IsAny<NotificationType>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<NotificationReferenceType?>()))
                .Returns(Task.CompletedTask);

            // Act
            await service.VoidFineAsync("admin-123", fine.Id);

            // Assert
            fine.Status.Should().Be(FineStatus.Voided);
            fine.PaymentMethod.Should().BeNull();
            fine.PaymentDescription.Should().BeNull();
            fine.PaymentProofImageUrl.Should().BeNull();
            fine.ProofSubmittedAt.Should().BeNull();
            fine.RejectionReason.Should().BeNull();

            _fineRepositoryMock.Verify(r => r.Update(fine), Times.Once);
            _fineRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task VoidFineAsync_ShouldThrow_WhenFineIsPaid()
        {
            // Arrange
            var service = CreateService();

            var fine = CreateFine(status: FineStatus.Paid);

            _fineRepositoryMock
                .Setup(r => r.GetByIdAsync(fine.Id))
                .ReturnsAsync(fine);

            // Act
            Func<Task> act = async () => await service.VoidFineAsync("admin-123", fine.Id);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Cannot void a paid fine.");
        }

        [Fact]
        public async Task SubmitPaymentProofAsync_ShouldSubmitProof_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var fine = CreateFine(status: FineStatus.Unpaid);

            var dto = new SubmitPaymentProofDto
            {
                FineId = fine.Id,
                PaymentMethod = 0,
                PaymentDescription = "Paid via MobilePay",
                PaymentProofImageUrl = "https://example.com/proof.jpg"
            };

            _fineRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(fine.Id))
                .ReturnsAsync(fine);

            _fineRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _notificationServiceMock
                .Setup(n => n.SendAsync(
                    It.IsAny<string>(),
                    It.IsAny<NotificationType>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<NotificationReferenceType?>()))
                .Returns(Task.CompletedTask);

            _notificationServiceMock
                .Setup(n => n.SendToAdminsAsync(
                    It.IsAny<NotificationType>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<NotificationReferenceType?>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await service.SubmitPaymentProofAsync(fine.UserId, dto);

            // Assert
            fine.Status.Should().Be(FineStatus.PendingVerification);
            fine.PaymentMethod.Should().Be(dto.PaymentMethod);
            fine.PaymentDescription.Should().Be(dto.PaymentDescription);
            fine.PaymentProofImageUrl.Should().Be(dto.PaymentProofImageUrl);
            fine.ProofSubmittedAt.Should().NotBeNull();

            result.Id.Should().Be(fine.Id);
            result.IsMine.Should().BeTrue();

            _fineRepositoryMock.Verify(r => r.Update(fine), Times.Once);
            _fineRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SubmitPaymentProofAsync_ShouldThrow_WhenFineBelongsToAnotherUser()
        {
            // Arrange
            var service = CreateService();

            var fine = CreateFine(userId: "owner-user");

            var dto = new SubmitPaymentProofDto
            {
                FineId = fine.Id,
                PaymentMethod = 0,
                PaymentDescription = "Paid",
                PaymentProofImageUrl = "https://example.com/proof.jpg"
            };

            _fineRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(fine.Id))
                .ReturnsAsync(fine);

            // Act
            Func<Task> act = async () => await service.SubmitPaymentProofAsync("another-user", dto);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You can only pay your own fines.");
        }

        [Fact]
        public async Task VerifyPaymentAsync_ShouldApprovePayment_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var fine = CreateFine(status: FineStatus.PendingVerification);

            var dto = new AdminFineVerifyPaymentDto
            {
                IsApproved = true
            };

            _fineRepositoryMock
                .Setup(r => r.GetByIdAsync(fine.Id))
                .ReturnsAsync(fine);

            _fineRepositoryMock
                .Setup(r => r.ExistsPaidFineAsync(fine.UserId, fine.LoanId, fine.DisputeId))
                .ReturnsAsync(false);

            _fineRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _notificationServiceMock
                .Setup(n => n.SendAsync(
                    It.IsAny<string>(),
                    It.IsAny<NotificationType>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<NotificationReferenceType?>()))
                .Returns(Task.CompletedTask);

            _fineRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(fine.Id))
                .ReturnsAsync(fine);

            // Act
            var result = await service.VerifyPaymentAsync("admin-123", fine.Id, dto);

            // Assert
            fine.Status.Should().Be(FineStatus.Paid);
            fine.VerifiedByAdminId.Should().Be("admin-123");
            fine.PaidAt.Should().NotBeNull();

            result.Status.Should().Be(FineStatus.Paid);

            _fineRepositoryMock.Verify(r => r.Update(fine), Times.Once);
            _fineRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task VerifyPaymentAsync_ShouldThrow_WhenRejectingWithoutReason()
        {
            // Arrange
            var service = CreateService();

            var fine = CreateFine(status: FineStatus.PendingVerification);

            var dto = new AdminFineVerifyPaymentDto
            {
                IsApproved = false,
                RejectionReason = "   "
            };

            _fineRepositoryMock
                .Setup(r => r.GetByIdAsync(fine.Id))
                .ReturnsAsync(fine);

            // Act
            Func<Task> act = async () => await service.VerifyPaymentAsync("admin-123", fine.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("A rejection reason is required when rejecting payment.");
        }

        [Fact]
        public async Task GetFineByIdAsync_ShouldThrow_WhenUserDoesNotOwnFine()
        {
            // Arrange
            var service = CreateService();

            var fine = CreateFine(userId: "owner-user");

            _fineRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(fine.Id))
                .ReturnsAsync(fine);

            // Act
            Func<Task> act = async () => await service.GetFineByIdAsync(fine.Id, "another-user");

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You do not have access to this fine.");
        }

        [Fact]
        public async Task GetFineStatsAsync_ShouldReturnStats()
        {
            // Arrange
            var service = CreateService();

            _fineRepositoryMock
                .Setup(r => r.GetStatusCountsAsync())
                .ReturnsAsync(new Dictionary<FineStatus, int>
                {
                    [FineStatus.Unpaid] = 3,
                    [FineStatus.PendingVerification] = 2,
                    [FineStatus.Paid] = 5
                });

            _fineRepositoryMock
                .Setup(r => r.GetTypeCountsAsync())
                .ReturnsAsync(new Dictionary<FineType, int>
                {
                    [FineType.Custom] = 4,
                    [FineType.ResultedByDispute] = 6
                });

            _fineRepositoryMock
                .Setup(r => r.GetOutstandingTotalAsync())
                .ReturnsAsync(1000m);

            _fineRepositoryMock
                .Setup(r => r.GetIssuedThisMonthCountAsync())
                .ReturnsAsync(7);

            // Act
            var result = await service.GetFineStatsAsync();

            // Assert
            result.TotalUnpaid.Should().Be(5);
            result.PendingProofReview.Should().Be(2);
            result.TotalOutstandingAmount.Should().Be(1000m);
            result.IssuedThisMonth.Should().Be(7);
        }
    }
}