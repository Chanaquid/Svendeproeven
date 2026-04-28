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
    public class DisputeServiceTests
    {
        private readonly Mock<IDisputeRepository> _disputeRepositoryMock = new();
        private readonly Mock<ILoanRepository> _loanRepositoryMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IFineRepository> _fineRepositoryMock = new();
        private readonly Mock<IScoreHistoryRepository> _scoreHistoryRepositoryMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

        public DisputeServiceTests()
        {
            _userManagerMock = MockUserManager();
        }

        private DisputeService CreateService()
        {
            return new DisputeService(
                _disputeRepositoryMock.Object,
                _loanRepositoryMock.Object,
                _userRepositoryMock.Object,
                _fineRepositoryMock.Object,
                _scoreHistoryRepositoryMock.Object,
                _notificationServiceMock.Object,
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

        private static ApplicationUser CreateUser(string id, string username)
        {
            return new ApplicationUser
            {
                Id = id,
                UserName = username,
                FullName = $"{username} FullName",
                AvatarUrl = $"{username}.png",
                Score = 100
            };
        }

        private static Loan CreateCompletedLoan()
        {
            var borrower = CreateUser("borrower-123", "borrower");
            var lender = CreateUser("lender-123", "lender");

            return new Loan
            {
                Id = 1,
                BorrowerId = borrower.Id,
                Borrower = borrower,
                LenderId = lender.Id,
                Lender = lender,
                ItemId = 10,
                Item = new Item
                {
                    Id = 10,
                    Title = "Test Item"
                },
                Status = LoanStatus.Completed,
                DisputeDeadline = DateTime.UtcNow.AddDays(7),
                SnapshotPhotos = new List<LoanSnapshotPhoto>()
            };
        }

        private static Dispute CreateDispute()
        {
            var loan = CreateCompletedLoan();

            return new Dispute
            {
                Id = 1,
                LoanId = loan.Id,
                Loan = loan,
                FiledById = loan.BorrowerId,
                FiledBy = loan.Borrower,
                FiledAs = 0,
                Description = "Item was damaged",
                Status = DisputeStatus.AwaitingResponse,
                ResponseDeadline = DateTime.UtcNow.AddHours(72),
                CreatedAt = DateTime.UtcNow,
                Photos = new List<DisputePhoto>(),
                Fines = new List<Fine>()
            };
        }

        [Fact]
        public async Task CreateDisputeAsync_ShouldThrow_WhenLoanDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var dto = new CreateDisputeDto
            {
                LoanId = 1,
                FiledAs = 0,
                Description = "Problem"
            };

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(dto.LoanId))
                .ReturnsAsync((Loan?)null);

            // Act
            Func<Task> act = async () =>
                await service.CreateDisputeAsync("borrower-123", dto);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Loan not found.");
        }

        [Fact]
        public async Task CreateDisputeAsync_ShouldThrow_WhenLoanIsNotCompleted()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateCompletedLoan();
            loan.Status = LoanStatus.Active;

            var dto = new CreateDisputeDto
            {
                LoanId = loan.Id,
                FiledAs = 0,
                Description = "Problem"
            };

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(loan.Id))
                .ReturnsAsync(loan);

            // Act
            Func<Task> act = async () =>
                await service.CreateDisputeAsync(loan.BorrowerId, dto);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Disputes can only be filed for completed loans.");
        }

        [Fact]
        public async Task CreateDisputeAsync_ShouldThrow_WhenUserIsNotParticipant()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateCompletedLoan();

            var dto = new CreateDisputeDto
            {
                LoanId = loan.Id,
                FiledAs = 0,
                Description = "Problem"
            };

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(loan.Id))
                .ReturnsAsync(loan);

            // Act
            Func<Task> act = async () =>
                await service.CreateDisputeAsync("stranger-user", dto);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You are not a participant in this loan.");
        }

        [Fact]
        public async Task CancelDisputeAsync_ShouldCancelDispute_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var dispute = CreateDispute();

            _disputeRepositoryMock
                .Setup(r => r.GetByIdAsync(dispute.Id))
                .ReturnsAsync(dispute);

            _disputeRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.CancelDisputeAsync(dispute.FiledById, dispute.Id);

            // Assert
            dispute.Status.Should().Be(DisputeStatus.Cancelled);

            _disputeRepositoryMock.Verify(r => r.Update(dispute), Times.Once);
            _disputeRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CancelDisputeAsync_ShouldThrow_WhenUserIsNotFiler()
        {
            // Arrange
            var service = CreateService();

            var dispute = CreateDispute();

            _disputeRepositoryMock
                .Setup(r => r.GetByIdAsync(dispute.Id))
                .ReturnsAsync(dispute);

            // Act
            Func<Task> act = async () =>
                await service.CancelDisputeAsync("another-user", dispute.Id);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Only the filer can cancel this dispute.");

            _disputeRepositoryMock.Verify(r => r.Update(It.IsAny<Dispute>()), Times.Never);
        }

        [Fact]
        public async Task AddFiledByPhotoUrlAsync_ShouldAddPhoto_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var dispute = CreateDispute();
            var user = dispute.FiledBy!;

            _disputeRepositoryMock
                .Setup(r => r.GetByIdAsync(dispute.Id))
                .ReturnsAsync(dispute);

            _disputeRepositoryMock
                .Setup(r => r.AddPhotoAsync(It.IsAny<DisputePhoto>()))
                .Callback<DisputePhoto>(photo =>
                {
                    photo.Id = 10;
                })
                .Returns(Task.CompletedTask);

            _disputeRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _userManagerMock
                .Setup(m => m.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            var result = await service.AddFiledByPhotoUrlAsync(
                user.Id,
                dispute.Id,
                "https://example.com/photo.jpg",
                null);

            // Assert
            result.Id.Should().Be(10);
            result.DisputeId.Should().Be(dispute.Id);
            result.PhotoUrl.Should().Be("https://example.com/photo.jpg");
            result.SubmittedById.Should().Be(user.Id);
            result.SubmittedByName.Should().Be(user.FullName);

            _disputeRepositoryMock.Verify(r => r.AddPhotoAsync(It.Is<DisputePhoto>(p =>
                p.DisputeId == dispute.Id &&
                p.SubmittedById == user.Id &&
                p.PhotoUrl == "https://example.com/photo.jpg"
            )), Times.Once);

            _disputeRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SubmitResponseAsync_ShouldSubmitResponse_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var dispute = CreateDispute();
            var respondentId = dispute.Loan.LenderId;

            var dto = new SubmitDisputeResponseDto
            {
                ResponseDescription = "Here is my response"
            };

            _disputeRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(dispute.Id))
                .ReturnsAsync(dispute);

            _disputeRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _notificationServiceMock
                .Setup(n => n.SendToAdminsAsync(
                    It.IsAny<NotificationType>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<NotificationReferenceType?>()))
                .Returns(Task.CompletedTask);

            _disputeRepositoryMock
                .Setup(r => r.GetByIdAsync(dispute.Id))
                .ReturnsAsync(dispute);

            _loanRepositoryMock
                .Setup(r => r.GetByIdAsync(dispute.LoanId))
                .ReturnsAsync(dispute.Loan);

            // Act
            var result = await service.SubmitResponseAsync(respondentId, dispute.Id, dto);

            // Assert
            dispute.RespondedById.Should().Be(respondentId);
            dispute.ResponseDescription.Should().Be(dto.ResponseDescription);
            dispute.Status.Should().Be(DisputeStatus.PendingAdminReview);
            result.Id.Should().Be(dispute.Id);

            _disputeRepositoryMock.Verify(r => r.Update(dispute), Times.Once);
            _disputeRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SubmitResponseAsync_ShouldThrow_WhenFilerRespondsToOwnDispute()
        {
            // Arrange
            var service = CreateService();

            var dispute = CreateDispute();

            var dto = new SubmitDisputeResponseDto
            {
                ResponseDescription = "Response"
            };

            _disputeRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(dispute.Id))
                .ReturnsAsync(dispute);

            // Act
            Func<Task> act = async () =>
                await service.SubmitResponseAsync(dispute.FiledById, dispute.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("You cannot respond to your own dispute.");
        }

        [Fact]
        public async Task CanUserFileDisputeAsync_ShouldReturnTrue_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateCompletedLoan();

            _loanRepositoryMock
                .Setup(r => r.GetByIdAsync(loan.Id))
                .ReturnsAsync(loan);

            _disputeRepositoryMock
                .Setup(r => r.HasUserFiledDisputeForLoanAsync(loan.Id, loan.BorrowerId))
                .ReturnsAsync(false);

            _disputeRepositoryMock
                .Setup(r => r.HasActiveDisputeAsync(loan.Id))
                .ReturnsAsync(false);

            _disputeRepositoryMock
                .Setup(r => r.GetDisputeCountForLoanAsync(loan.Id))
                .ReturnsAsync(0);

            // Act
            var result = await service.CanUserFileDisputeAsync(loan.BorrowerId, loan.Id);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CanUserFileDisputeAsync_ShouldReturnFalse_WhenLoanIsNotCompleted()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateCompletedLoan();
            loan.Status = LoanStatus.Active;

            _loanRepositoryMock
                .Setup(r => r.GetByIdAsync(loan.Id))
                .ReturnsAsync(loan);

            // Act
            var result = await service.CanUserFileDisputeAsync(loan.BorrowerId, loan.Id);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ProcessExpiredDisputesAsync_ShouldMarkExpiredDisputesAsPastDeadline()
        {
            // Arrange
            var service = CreateService();

            var dispute1 = CreateDispute();
            var dispute2 = CreateDispute();
            dispute2.Id = 2;

            var expiredDisputes = new List<Dispute>
            {
                dispute1,
                dispute2
            };

            _disputeRepositoryMock
                .Setup(r => r.GetExpiredAwaitingResponseAsync())
                .ReturnsAsync(expiredDisputes);

            _disputeRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await service.ProcessExpiredDisputesAsync();

            // Assert
            result.Should().Be(2);
            dispute1.Status.Should().Be(DisputeStatus.PastDeadline);
            dispute2.Status.Should().Be(DisputeStatus.PastDeadline);

            _disputeRepositoryMock.Verify(r => r.Update(dispute1), Times.Once);
            _disputeRepositoryMock.Verify(r => r.Update(dispute2), Times.Once);
            _disputeRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}