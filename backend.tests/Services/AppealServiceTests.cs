using backend.Data;
using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace backend.Tests.Services
{
    public class AppealServiceTests
    {
        private readonly Mock<IAppealRepository> _appealRepositoryMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<IFineRepository> _fineRepositoryMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly Mock<IScoreHistoryRepository> _scoreHistoryRepositoryMock = new();
        private readonly ApplicationDbContext _dbContext;

        public AppealServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options);
        }

        private AppealService CreateService()
        {
            return new AppealService(
                _appealRepositoryMock.Object,
                _userRepositoryMock.Object,
                _fineRepositoryMock.Object,
                _notificationServiceMock.Object,
                _scoreHistoryRepositoryMock.Object,
                _dbContext
            );
        }

        private static ApplicationUser CreateUser(
            string id = "user-123",
            int score = 10)
        {
            return new ApplicationUser
            {
                Id = id,
                UserName = "testuser",
                FullName = "Test User",
                Email = "user@example.com",
                Score = score
            };
        }

        private static Appeal CreateScoreAppeal(
            int id = 1,
            string userId = "user-123",
            AppealStatus status = AppealStatus.Pending)
        {
            var user = CreateUser(userId, score: 10);

            return new Appeal
            {
                Id = id,
                UserId = userId,
                User = user,
                Message = "Please restore my score",
                AppealType = AppealType.Score,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };
        }

        private static Appeal CreateFineAppeal(
            int id = 1,
            string userId = "user-123",
            int fineId = 5,
            AppealStatus status = AppealStatus.Pending)
        {
            var user = CreateUser(userId, score: 50);

            return new Appeal
            {
                Id = id,
                UserId = userId,
                User = user,
                FineId = fineId,
                Fine = new Fine
                {
                    Id = fineId,
                    UserId = userId,
                    Amount = 500,
                    Status = FineStatus.Unpaid
                },
                Message = "I disagree with this fine",
                AppealType = AppealType.Fine,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task CreateScoreAppealAsync_ShouldCreateAppeal_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser(score: 10);

            var dto = new CreateScoreAppealDto
            {
                Message = " Please review my score "
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _appealRepositoryMock
                .Setup(r => r.HasPendingScoreAppealAsync(user.Id))
                .ReturnsAsync(false);

            _appealRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Appeal>()))
                .Callback<Appeal>(appeal =>
                {
                    appeal.Id = 1;
                    appeal.User = user;
                })
                .Returns(Task.CompletedTask);

            _appealRepositoryMock
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

            _appealRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(1))
                .ReturnsAsync(CreateScoreAppeal(id: 1, userId: user.Id));

            // Act
            var result = await service.CreateScoreAppealAsync(user.Id, dto);

            // Assert
            result.Id.Should().Be(1);
            result.UserId.Should().Be(user.Id);
            result.AppealType.Should().Be(AppealType.Score);
            result.Status.Should().Be(AppealStatus.Pending);

            _appealRepositoryMock.Verify(r => r.AddAsync(It.Is<Appeal>(a =>
                a.UserId == user.Id &&
                a.Message == "Please review my score" &&
                a.AppealType == AppealType.Score &&
                a.Status == AppealStatus.Pending
            )), Times.Once);

            _appealRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);

            _notificationServiceMock.Verify(n => n.SendAsync(
                user.Id,
                NotificationType.AppealSubmitted,
                It.IsAny<string>(),
                1,
                NotificationReferenceType.Appeal
            ), Times.Once);
        }

        [Fact]
        public async Task CreateScoreAppealAsync_ShouldThrow_WhenUserScoreIs20OrAbove()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser(score: 20);

            var dto = new CreateScoreAppealDto
            {
                Message = "Please review"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            Func<Task> act = async () => await service.CreateScoreAppealAsync(user.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Your score is 20 or above. You do not need a score appeal.");

            _appealRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Appeal>()), Times.Never);
        }

        [Fact]
        public async Task CreateScoreAppealAsync_ShouldThrow_WhenMessageIsEmpty()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser(score: 10);

            var dto = new CreateScoreAppealDto
            {
                Message = "   "
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _appealRepositoryMock
                .Setup(r => r.HasPendingScoreAppealAsync(user.Id))
                .ReturnsAsync(false);

            // Act
            Func<Task> act = async () => await service.CreateScoreAppealAsync(user.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("Appeal message cannot be empty.");

            _appealRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Appeal>()), Times.Never);
        }

        [Fact]
        public async Task CreateFineAppealAsync_ShouldCreateAppeal_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser(score: 50);

            var fine = new Fine
            {
                Id = 5,
                UserId = user.Id,
                Amount = 500,
                Status = FineStatus.Unpaid
            };

            var dto = new CreateFineAppealDto
            {
                FineId = fine.Id,
                Message = " This fine is wrong "
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _fineRepositoryMock
                .Setup(r => r.GetByIdAsync(fine.Id))
                .ReturnsAsync(fine);

            _appealRepositoryMock
                .Setup(r => r.HasFineAppealAsync(user.Id, fine.Id))
                .ReturnsAsync(false);

            _appealRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Appeal>()))
                .Callback<Appeal>(appeal =>
                {
                    appeal.Id = 1;
                    appeal.User = user;
                    appeal.Fine = fine;
                })
                .Returns(Task.CompletedTask);

            _appealRepositoryMock
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

            _appealRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(1))
                .ReturnsAsync(CreateFineAppeal(id: 1, userId: user.Id, fineId: fine.Id));

            // Act
            var result = await service.CreateFineAppealAsync(user.Id, dto);

            // Assert
            result.Id.Should().Be(1);
            result.UserId.Should().Be(user.Id);
            result.FineId.Should().Be(fine.Id);
            result.AppealType.Should().Be(AppealType.Fine);
            result.Status.Should().Be(AppealStatus.Pending);

            _appealRepositoryMock.Verify(r => r.AddAsync(It.Is<Appeal>(a =>
                a.UserId == user.Id &&
                a.FineId == fine.Id &&
                a.Message == "This fine is wrong" &&
                a.AppealType == AppealType.Fine &&
                a.Status == AppealStatus.Pending
            )), Times.Once);

            _appealRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateFineAppealAsync_ShouldThrow_WhenFineBelongsToAnotherUser()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser("user-123");

            var fine = new Fine
            {
                Id = 5,
                UserId = "another-user",
                Amount = 500,
                Status = FineStatus.Unpaid
            };

            var dto = new CreateFineAppealDto
            {
                FineId = fine.Id,
                Message = "This fine is wrong"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _fineRepositoryMock
                .Setup(r => r.GetByIdAsync(fine.Id))
                .ReturnsAsync(fine);

            // Act
            Func<Task> act = async () => await service.CreateFineAppealAsync(user.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You can only appeal your own fines.");

            _appealRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Appeal>()), Times.Never);
        }

        [Fact]
        public async Task CancelAppealAsync_ShouldCancelPendingAppeal_WhenUserOwnsAppeal()
        {
            // Arrange
            var service = CreateService();

            var appeal = CreateScoreAppeal(status: AppealStatus.Pending);

            _appealRepositoryMock
                .Setup(r => r.GetByIdAsync(appeal.Id))
                .ReturnsAsync(appeal);

            _appealRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.CancelAppealAsync(appeal.Id, appeal.UserId);

            // Assert
            appeal.Status.Should().Be(AppealStatus.Cancelled);

            _appealRepositoryMock.Verify(r => r.Update(appeal), Times.Once);
            _appealRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CancelAppealAsync_ShouldThrow_WhenAppealDoesNotBelongToUser()
        {
            // Arrange
            var service = CreateService();

            var appeal = CreateScoreAppeal(userId: "owner-user");

            _appealRepositoryMock
                .Setup(r => r.GetByIdAsync(appeal.Id))
                .ReturnsAsync(appeal);

            // Act
            Func<Task> act = async () => await service.CancelAppealAsync(appeal.Id, "another-user");

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Only the creator can cancel this appeal.");

            _appealRepositoryMock.Verify(r => r.Update(It.IsAny<Appeal>()), Times.Never);
        }

        [Fact]
        public async Task DecideScoreAppealAsync_ShouldApproveAppealAndUpdateScore_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var appeal = CreateScoreAppeal();
            appeal.User.Score = 10;

            var admin = CreateUser("admin-123", score: 80);

            var dto = new AdminDecidesScoreAppealDto
            {
                IsApproved = true,
                NewScore = 30,
                AdminNote = "Approved"
            };

            _appealRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(appeal.Id))
                .ReturnsAsync(appeal);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(admin.Id))
                .ReturnsAsync(admin);

            _scoreHistoryRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<ScoreHistory>()))
                .Returns(Task.CompletedTask);

            _userRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _appealRepositoryMock
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
            var result = await service.DecideScoreAppealAsync(appeal.Id, admin.Id, dto);

            // Assert
            appeal.Status.Should().Be(AppealStatus.Approved);
            appeal.ResolvedByAdminId.Should().Be(admin.Id);
            appeal.RestoredScore.Should().Be(30);
            appeal.User.Score.Should().Be(30);

            result.Status.Should().Be(AppealStatus.Approved);
            result.RestoredScore.Should().Be(30);

            _scoreHistoryRepositoryMock.Verify(r => r.AddAsync(It.Is<ScoreHistory>(h =>
                h.UserId == appeal.UserId &&
                h.PointsChanged == 20 &&
                h.ScoreAfterChange == 30 &&
                h.Reason == ScoreChangeReason.AppealOutcome
            )), Times.Once);

            _userRepositoryMock.Verify(r => r.Update(appeal.User), Times.Once);
            _appealRepositoryMock.Verify(r => r.Update(appeal), Times.Once);
        }

        [Fact]
        public async Task DecideScoreAppealAsync_ShouldThrow_WhenRejectingWithoutReason()
        {
            // Arrange
            var service = CreateService();

            var appeal = CreateScoreAppeal();
            var admin = CreateUser("admin-123", score: 80);

            var dto = new AdminDecidesScoreAppealDto
            {
                IsApproved = false,
                AdminNote = "   "
            };

            _appealRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(appeal.Id))
                .ReturnsAsync(appeal);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(admin.Id))
                .ReturnsAsync(admin);

            // Act
            Func<Task> act = async () => await service.DecideScoreAppealAsync(appeal.Id, admin.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("A reason is required when rejecting an appeal.");

            _appealRepositoryMock.Verify(r => r.Update(It.IsAny<Appeal>()), Times.Never);
        }
    }
}