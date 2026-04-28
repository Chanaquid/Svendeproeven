using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace backend.Tests.Services
{
    public class ScoreHistoryServiceTests
    {
        private readonly Mock<IScoreHistoryRepository> _scoreHistoryRepositoryMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();

        private ScoreHistoryService CreateService()
        {
            return new ScoreHistoryService(
                _scoreHistoryRepositoryMock.Object,
                _userRepositoryMock.Object
            );
        }

        private static ApplicationUser CreateUser(
            string id = "user-123",
            int score = 50)
        {
            return new ApplicationUser
            {
                Id = id,
                FullName = "Test User",
                UserName = "testuser",
                Score = score
            };
        }

        private static ScoreHistory CreateScoreHistory()
        {
            return new ScoreHistory
            {
                Id = 1,
                UserId = "user-123",
                PointsChanged = 10,
                ScoreAfterChange = 60,
                Reason = ScoreChangeReason.AdminAdjustment,
                Note = "Manual adjustment",
                CreatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task GetMyHistoryAsync_ShouldReturnPagedHistory()
        {
            // Arrange
            var service = CreateService();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var history = CreateScoreHistory();

            _scoreHistoryRepositoryMock
                .Setup(r => r.GetByUserIdAsync("user-123", null, request))
                .ReturnsAsync(new PagedResult<ScoreHistory>
                {
                    Items = new List<ScoreHistory> { history },
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 10
                });

            // Act
            var result = await service.GetMyHistoryAsync("user-123", null, request);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items[0].Id.Should().Be(history.Id);
            result.Items[0].PointsChanged.Should().Be(history.PointsChanged);
            result.Items[0].ScoreAfterChange.Should().Be(history.ScoreAfterChange);
            result.Items[0].Reason.Should().Be(history.Reason);
            result.TotalCount.Should().Be(1);
        }

        

        [Fact]
        public async Task GetByUserIdAsync_ShouldThrow_WhenUserDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync("missing-user"))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            Func<Task> act = async () =>
                await service.GetByUserIdAsync("missing-user", null, request);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("User not found.");
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldReturnPagedHistory_WhenUserExists()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser();
            var history = CreateScoreHistory();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _scoreHistoryRepositoryMock
                .Setup(r => r.GetByUserIdAsync(user.Id, null, request))
                .ReturnsAsync(new PagedResult<ScoreHistory>
                {
                    Items = new List<ScoreHistory> { history },
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 10
                });

            // Act
            var result = await service.GetByUserIdAsync(user.Id, null, request);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items[0].Id.Should().Be(history.Id);
            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPagedHistory()
        {
            // Arrange
            var service = CreateService();

            var history = CreateScoreHistory();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            _scoreHistoryRepositoryMock
                .Setup(r => r.GetAllAsync(null, request))
                .ReturnsAsync(new PagedResult<ScoreHistory>
                {
                    Items = new List<ScoreHistory> { history },
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 10
                });

            // Act
            var result = await service.GetAllAsync(null, request);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items[0].Id.Should().Be(history.Id);
            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetScoreSummaryByUserIdAsync_ShouldThrow_WhenUserDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync("missing-user"))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            Func<Task> act = async () =>
                await service.GetScoreSummaryByUserIdAsync("missing-user");

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("User not found.");
        }



        [Fact]
        public async Task AdminAdjustScoreAsync_ShouldThrow_WhenUserDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var dto = new AdminAdjustScoreDto
            {
                UserId = "missing-user",
                PointsChanged = 10,
                Reason = ScoreChangeReason.AdminAdjustment,
                Note = "Manual adjustment"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(dto.UserId))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            Func<Task> act = async () => await service.AdminAdjustScoreAsync("admin-123", dto);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("User not found.");

            _scoreHistoryRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ScoreHistory>()), Times.Never);
        }

        [Fact]
        public async Task AdminAdjustScoreAsync_ShouldIncreaseUserScore_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser(score: 50);

            var dto = new AdminAdjustScoreDto
            {
                UserId = user.Id,
                PointsChanged = 20,
                Reason = ScoreChangeReason.AdminAdjustment,
                Note = " Good behavior "
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _scoreHistoryRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<ScoreHistory>()))
                .Returns(Task.CompletedTask);

            _scoreHistoryRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.AdminAdjustScoreAsync("admin-123", dto);

            // Assert
            user.Score.Should().Be(70);

            _userRepositoryMock.Verify(r => r.Update(user), Times.Once);

            _scoreHistoryRepositoryMock.Verify(r => r.AddAsync(It.Is<ScoreHistory>(h =>
                h.UserId == user.Id &&
                h.PointsChanged == 20 &&
                h.ScoreAfterChange == 70 &&
                h.Reason == ScoreChangeReason.AdminAdjustment &&
                h.Note == "Good behavior"
            )), Times.Once);

            _scoreHistoryRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task AdminAdjustScoreAsync_ShouldClampScoreTo100()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser(score: 90);

            var dto = new AdminAdjustScoreDto
            {
                UserId = user.Id,
                PointsChanged = 20,
                Reason = ScoreChangeReason.AdminAdjustment,
                Note = "Increase"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _scoreHistoryRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<ScoreHistory>()))
                .Returns(Task.CompletedTask);

            _scoreHistoryRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.AdminAdjustScoreAsync("admin-123", dto);

            // Assert
            user.Score.Should().Be(100);

            _scoreHistoryRepositoryMock.Verify(r => r.AddAsync(It.Is<ScoreHistory>(h =>
                h.PointsChanged == 10 &&
                h.ScoreAfterChange == 100
            )), Times.Once);
        }

        [Fact]
        public async Task AdminAdjustScoreAsync_ShouldClampScoreTo0()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser(score: 5);

            var dto = new AdminAdjustScoreDto
            {
                UserId = user.Id,
                PointsChanged = -20,
                Reason = ScoreChangeReason.AdminAdjustment,
                Note = "Decrease"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _scoreHistoryRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<ScoreHistory>()))
                .Returns(Task.CompletedTask);

            _scoreHistoryRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.AdminAdjustScoreAsync("admin-123", dto);

            // Assert
            user.Score.Should().Be(0);

            _scoreHistoryRepositoryMock.Verify(r => r.AddAsync(It.Is<ScoreHistory>(h =>
                h.PointsChanged == -5 &&
                h.ScoreAfterChange == 0
            )), Times.Once);
        }

        [Fact]
        public async Task AdminAdjustScoreAsync_ShouldThrow_WhenUserScoreAlready100AndTryingToIncrease()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser(score: 100);

            var dto = new AdminAdjustScoreDto
            {
                UserId = user.Id,
                PointsChanged = 10,
                Reason = ScoreChangeReason.AdminAdjustment,
                Note = "Increase"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            Func<Task> act = async () => await service.AdminAdjustScoreAsync("admin-123", dto);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("User score is already at 100.");

            _scoreHistoryRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ScoreHistory>()), Times.Never);
        }

        [Fact]
        public async Task AdminAdjustScoreAsync_ShouldThrow_WhenUserScoreAlready0AndTryingToDecrease()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser(score: 0);

            var dto = new AdminAdjustScoreDto
            {
                UserId = user.Id,
                PointsChanged = -10,
                Reason = ScoreChangeReason.AdminAdjustment,
                Note = "Decrease"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            Func<Task> act = async () => await service.AdminAdjustScoreAsync("admin-123", dto);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("User score is already at 0.");

            _scoreHistoryRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ScoreHistory>()), Times.Never);
        }
    }
}