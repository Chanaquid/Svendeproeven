using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace backend.Tests.Services
{
    public class LoanMessageServiceTests
    {
        private readonly Mock<ILoanMessageRepository> _loanMessageRepositoryMock = new();
        private readonly Mock<ILoanRepository> _loanRepositoryMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly Mock<IHubContext<LoanChatHub>> _hubContextMock = new();
        private readonly Mock<IHubClients> _hubClientsMock = new();
        private readonly Mock<IClientProxy> _clientProxyMock = new();
        private readonly Mock<IOnlineTracker> _onlineTrackerMock = new();
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock = new(
            Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);

        private LoanMessageService CreateService()
        {
            _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);
            _hubClientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
            _clientProxyMock
                .Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            return new LoanMessageService(
                _loanMessageRepositoryMock.Object,
                _loanRepositoryMock.Object,
                _userRepositoryMock.Object,
                _notificationServiceMock.Object,
                _hubContextMock.Object,
                _onlineTrackerMock.Object,
                _userManagerMock.Object
            );
        }

        private static ApplicationUser CreateUser(string id, string name)
        {
            return new ApplicationUser
            {
                Id = id,
                FullName = name,
                AvatarUrl = $"{name}.png"
            };
        }

        private static Loan CreateLoan(LoanStatus status = LoanStatus.Active)
        {
            var borrower = CreateUser("borrower-123", "Borrower User");
            var lender = CreateUser("lender-123", "Lender User");

            return new Loan
            {
                Id = 1,
                BorrowerId = borrower.Id,
                Borrower = borrower,
                LenderId = lender.Id,
                Lender = lender,
                Status = status
            };
        }

        [Fact]
        public async Task SendMessageAsync_ShouldThrow_WhenLoanDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var dto = new SendLoanMessageDto
            {
                Content = "Hello"
            };

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(1))
                .ReturnsAsync((Loan?)null);

            // Act
            Func<Task> act = async () =>
                await service.SendMessageAsync(1, "borrower-123", dto, isAdmin: false);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Loan not found.");
        }

        [Fact]
        public async Task SendMessageAsync_ShouldThrow_WhenLoanIsRejected()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateLoan(LoanStatus.Rejected);

            var dto = new SendLoanMessageDto
            {
                Content = "Hello"
            };

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(loan.Id))
                .ReturnsAsync(loan);

            // Act
            Func<Task> act = async () =>
                await service.SendMessageAsync(loan.Id, loan.BorrowerId, dto, isAdmin: false);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Messaging is not available for rejected or cancelled loans.");

            _loanMessageRepositoryMock.Verify(r => r.AddAsync(It.IsAny<LoanMessage>()), Times.Never);
        }

        [Fact]
        public async Task SendMessageAsync_ShouldThrow_WhenUserIsNotBorrowerOrLender()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateLoan(LoanStatus.Active);

            var dto = new SendLoanMessageDto
            {
                Content = "Hello"
            };

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(loan.Id))
                .ReturnsAsync(loan);

            // Act
            Func<Task> act = async () =>
                await service.SendMessageAsync(loan.Id, "stranger-user", dto, isAdmin: false);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Only the borrower and lender can send messages on this loan.");

            _loanMessageRepositoryMock.Verify(r => r.AddAsync(It.IsAny<LoanMessage>()), Times.Never);
        }

        [Fact]
        public async Task SendMessageAsync_ShouldCreateMessage_WhenBorrowerSendsMessage()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateLoan(LoanStatus.Active);

            var dto = new SendLoanMessageDto
            {
                Content = " Hello loan message "
            };

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(loan.Id))
                .ReturnsAsync(loan);

            _loanMessageRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<LoanMessage>()))
                .Callback<LoanMessage>(message =>
                {
                    message.Id = 10;
                })
                .Returns(Task.CompletedTask);

            _loanMessageRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await service.SendMessageAsync(
                loan.Id,
                loan.BorrowerId,
                dto,
                isAdmin: false);

            // Assert
            result.Id.Should().Be(10);
            result.LoanId.Should().Be(loan.Id);
            result.SenderId.Should().Be(loan.BorrowerId);
            result.SenderName.Should().Be(loan.Borrower!.FullName);
            result.Content.Should().Be("Hello loan message");
            result.IsMine.Should().BeTrue();
            result.IsRead.Should().BeFalse();

            _loanMessageRepositoryMock.Verify(r => r.AddAsync(It.Is<LoanMessage>(m =>
                m.LoanId == loan.Id &&
                m.SenderId == loan.BorrowerId &&
                m.Content == "Hello loan message" &&
                m.IsRead == false
            )), Times.Once);

            _loanMessageRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_ShouldAllowAdminOnlyWhenLoanIsAdminPending()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateLoan(LoanStatus.AdminPending);
            var admin = CreateUser("admin-123", "Admin User");

            var dto = new SendLoanMessageDto
            {
                Content = "Admin message"
            };

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(loan.Id))
                .ReturnsAsync(loan);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(admin.Id))
                .ReturnsAsync(admin);

            _loanMessageRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<LoanMessage>()))
                .Callback<LoanMessage>(message =>
                {
                    message.Id = 20;
                })
                .Returns(Task.CompletedTask);

            _loanMessageRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await service.SendMessageAsync(
                loan.Id,
                admin.Id,
                dto,
                isAdmin: true);

            // Assert
            result.Id.Should().Be(20);
            result.SenderId.Should().Be(admin.Id);
            result.SenderName.Should().Be(admin.FullName);
            result.Content.Should().Be("Admin message");

            _loanMessageRepositoryMock.Verify(r => r.AddAsync(It.IsAny<LoanMessage>()), Times.Once);
            _loanMessageRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_ShouldThrow_WhenAdminMessagesOutsideAdminPending()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateLoan(LoanStatus.Active);

            var dto = new SendLoanMessageDto
            {
                Content = "Admin message"
            };

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(loan.Id))
                .ReturnsAsync(loan);

            // Act
            Func<Task> act = async () =>
                await service.SendMessageAsync(loan.Id, "admin-123", dto, isAdmin: true);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Admins can only send messages while the loan is pending admin review.");
        }

        [Fact]
        public async Task GetMessagesAsync_ShouldThrow_WhenUserHasNoAccess()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateLoan();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            _loanRepositoryMock
                .Setup(r => r.GetByIdAsync(loan.Id))
                .ReturnsAsync(loan);

            // Act
            Func<Task> act = async () =>
                await service.GetMessagesAsync(loan.Id, "stranger-user", isAdmin: false, request);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You do not have access to this loan's messages.");
        }

        [Fact]
        public async Task GetMessagesAsync_ShouldReturnMessages_WhenUserHasAccess()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateLoan();
            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var message = new LoanMessage
            {
                Id = 5,
                LoanId = loan.Id,
                SenderId = loan.BorrowerId,
                Sender = loan.Borrower,
                Content = "Hello",
                IsRead = false,
                SentAt = DateTime.UtcNow
            };

            _loanRepositoryMock
                .Setup(r => r.GetByIdAsync(loan.Id))
                .ReturnsAsync(loan);

            _loanMessageRepositoryMock
                .Setup(r => r.GetByLoanIdAsync(loan.Id, request))
                .ReturnsAsync(new PagedResult<LoanMessage>
                {
                    Items = new List<LoanMessage> { message },
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 10
                });

            // Act
            var result = await service.GetMessagesAsync(
                loan.Id,
                loan.BorrowerId,
                isAdmin: false,
                request);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items[0].Id.Should().Be(message.Id);
            result.Items[0].Content.Should().Be("Hello");
            result.Items[0].IsMine.Should().BeTrue();
            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task MarkAsReadAsync_ShouldCallRepository_WhenUserHasAccess()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateLoan();

            var dto = new MarkLoanMessagesReadDto
            {
                UpToMessageId = 10
            };

            _loanRepositoryMock
                .Setup(r => r.GetByIdAsync(loan.Id))
                .ReturnsAsync(loan);

            _loanMessageRepositoryMock
                .Setup(r => r.MarkAsReadAsync(loan.Id, loan.BorrowerId, dto.UpToMessageId))
                .Returns(Task.CompletedTask);

            // Act
            await service.MarkAsReadAsync(loan.Id, loan.BorrowerId, dto);

            // Assert
            _loanMessageRepositoryMock.Verify(
                r => r.MarkAsReadAsync(loan.Id, loan.BorrowerId, dto.UpToMessageId),
                Times.Once);
        }

        [Fact]
        public async Task GetUnreadCountAsync_ShouldReturnCount_WhenUserHasAccess()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateLoan();

            _loanRepositoryMock
                .Setup(r => r.GetByIdAsync(loan.Id))
                .ReturnsAsync(loan);

            _loanMessageRepositoryMock
                .Setup(r => r.GetUnreadCountAsync(loan.Id, loan.BorrowerId))
                .ReturnsAsync(3);

            // Act
            var result = await service.GetUnreadCountAsync(loan.Id, loan.BorrowerId);

            // Assert
            result.Should().Be(3);
        }

        [Fact]
        public async Task IsPartyToLoanAsync_ShouldReturnTrue_WhenUserIsBorrowerOrLender()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateLoan();

            _loanRepositoryMock
                .Setup(r => r.GetByIdAsync(loan.Id))
                .ReturnsAsync(loan);

            // Act
            var result = await service.IsPartyToLoanAsync(loan.Id, loan.BorrowerId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsPartyToLoanAsync_ShouldReturnFalse_WhenLoanDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            _loanRepositoryMock
                .Setup(r => r.GetByIdAsync(99))
                .ReturnsAsync((Loan?)null);

            // Act
            var result = await service.IsPartyToLoanAsync(99, "user-123");

            // Assert
            result.Should().BeFalse();
        }
    }
}