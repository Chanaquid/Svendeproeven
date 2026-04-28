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
    public class LoanServiceTests
    {
        private readonly Mock<ILoanRepository> _loanRepositoryMock = new();
        private readonly Mock<IItemRepository> _itemRepositoryMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly Mock<IFineRepository> _fineRepositoryMock = new();
        private readonly Mock<IScoreHistoryRepository> _scoreHistoryRepositoryMock = new();
        private readonly Mock<IItemReviewRepository> _itemReviewRepositoryMock = new();
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

        public LoanServiceTests()
        {
            _userManagerMock = MockUserManager();
        }

        private LoanService CreateService()
        {
            return new LoanService(
                _loanRepositoryMock.Object,
                _itemRepositoryMock.Object,
                _userRepositoryMock.Object,
                _notificationServiceMock.Object,
                _fineRepositoryMock.Object,
                _scoreHistoryRepositoryMock.Object,
                _itemReviewRepositoryMock.Object,
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

        private static ApplicationUser CreateUser(
            string id = "borrower-123",
            string name = "Borrower User",
            int score = 100,
            bool isVerified = true)
        {
            return new ApplicationUser
            {
                Id = id,
                FullName = name,
                UserName = name.Replace(" ", "").ToLower(),
                Email = $"{id}@test.com",
                AvatarUrl = "avatar.png",
                Score = score,
                IsVerified = isVerified
            };
        }

        private static Item CreateItem()
        {
            var owner = CreateUser("owner-123", "Owner User");

            return new Item
            {
                Id = 1,
                OwnerId = owner.Id,
                Owner = owner,
                Title = "Hammer",
                Slug = "hammer",
                Description = "Good hammer",
                PricePerDay = 20,
                CurrentValue = 200,
                IsFree = false,
                RequiresVerification = false,
                Condition = ItemCondition.Good,
                Status = ItemStatus.Approved,
                Availability = ItemAvailability.Available,
                IsActive = true,
                IsDeleted = false,
                AvailableFrom = DateTime.UtcNow.Date.AddDays(1),
                AvailableUntil = DateTime.UtcNow.Date.AddDays(10),
                Photos = new List<ItemPhoto>(),
                Loans = new List<Loan>(),
                Reviews = new List<ItemReview>()
            };
        }

        private static Loan CreateLoan(LoanStatus status = LoanStatus.Pending)
        {
            var borrower = CreateUser("borrower-123", "Borrower User");
            var lender = CreateUser("owner-123", "Owner User");
            var item = CreateItem();

            return new Loan
            {
                Id = 1,
                ItemId = item.Id,
                Item = item,
                BorrowerId = borrower.Id,
                Borrower = borrower,
                LenderId = lender.Id,
                Lender = lender,
                StartDate = DateTime.UtcNow.Date.AddDays(1),
                EndDate = DateTime.UtcNow.Date.AddDays(3),
                TotalPrice = 60,
                PricePerDaySnapshot = 20,
                Status = status,
                CreatedAt = DateTime.UtcNow,
                SnapshotPhotos = new List<LoanSnapshotPhoto>(),
                Fines = new List<Fine>()
            };
        }

        private static CreateLoanDto CreateValidCreateLoanDto()
        {
            return new CreateLoanDto
            {
                ItemId = 1,
                StartDate = DateTime.UtcNow.Date.AddDays(1),
                EndDate = DateTime.UtcNow.Date.AddDays(3),
                NoteToOwner = "Can I borrow this?"
            };
        }

        [Fact]
        public async Task CreateLoanAsync_ShouldThrow_WhenBorrowerDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var dto = CreateValidCreateLoanDto();

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync("missing-user"))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            Func<Task> act = async () => await service.CreateLoanAsync("missing-user", dto);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Borrower not found.");
        }

        [Fact]
        public async Task CreateLoanAsync_ShouldThrow_WhenItemDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var borrower = CreateUser();
            var dto = CreateValidCreateLoanDto();

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(borrower.Id))
                .ReturnsAsync(borrower);

            _itemRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(dto.ItemId))
                .ReturnsAsync((Item?)null);

            // Act
            Func<Task> act = async () => await service.CreateLoanAsync(borrower.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Item not found.");
        }

        [Fact]
        public async Task CreateLoanAsync_ShouldThrow_WhenBorrowingOwnItem()
        {
            // Arrange
            var service = CreateService();

            var borrower = CreateUser("owner-123", "Owner User");
            var item = CreateItem();
            var dto = CreateValidCreateLoanDto();

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(borrower.Id))
                .ReturnsAsync(borrower);

            _itemRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(dto.ItemId))
                .ReturnsAsync(item);

            // Act
            Func<Task> act = async () => await service.CreateLoanAsync(borrower.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("You cannot borrow your own item.");
        }

        [Fact]
        public async Task CreateLoanAsync_ShouldThrow_WhenBorrowerScoreIsTooLow()
        {
            // Arrange
            var service = CreateService();

            var borrower = CreateUser(score: 10);
            var item = CreateItem();
            var dto = CreateValidCreateLoanDto();

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(borrower.Id))
                .ReturnsAsync(borrower);

            _itemRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(dto.ItemId))
                .ReturnsAsync(item);

            _loanRepositoryMock
                .Setup(r => r.IsItemAvailableForDatesAsync(
                    dto.ItemId,
                    dto.StartDate.ToUniversalTime().Date,
                    dto.EndDate.ToUniversalTime().Date))
                .ReturnsAsync(true);

            // Act
            Func<Task> act = async () => await service.CreateLoanAsync(borrower.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Your score is too low to borrow items. Please appeal to restore your score.");
        }

        [Fact]
        public async Task CreateLoanAsync_ShouldCreateLoan_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var borrower = CreateUser(score: 100);
            var item = CreateItem();
            var dto = CreateValidCreateLoanDto();

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(borrower.Id))
                .ReturnsAsync(borrower);

            _itemRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(dto.ItemId))
                .ReturnsAsync(item);

            _loanRepositoryMock
                .Setup(r => r.IsItemAvailableForDatesAsync(
                    dto.ItemId,
                    dto.StartDate.ToUniversalTime().Date,
                    dto.EndDate.ToUniversalTime().Date))
                .ReturnsAsync(true);

            _loanRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Loan>()))
                .Callback<Loan>(loan =>
                {
                    loan.Id = 1;
                    loan.Item = item;
                    loan.Borrower = borrower;
                    loan.Lender = item.Owner!;
                    loan.SnapshotPhotos = new List<LoanSnapshotPhoto>();
                    loan.Fines = new List<Fine>();
                })
                .Returns(Task.CompletedTask);

            _loanRepositoryMock
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

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(1))
                .ReturnsAsync(CreateLoan(LoanStatus.Pending));

            // Act
            var result = await service.CreateLoanAsync(borrower.Id, dto);

            // Assert
            result.Id.Should().Be(1);
            result.Status.Should().Be(LoanStatus.Pending);

            _loanRepositoryMock.Verify(r => r.AddAsync(It.Is<Loan>(loan =>
                loan.ItemId == item.Id &&
                loan.BorrowerId == borrower.Id &&
                loan.LenderId == item.OwnerId &&
                loan.Status == LoanStatus.Pending &&
                loan.TotalPrice == 60
            )), Times.Once);

            _loanRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CancelLoanAsync_ShouldCancelLoan_WhenBorrowerOwnsLoan()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateLoan(LoanStatus.Pending);

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(loan.Id))
                .ReturnsAsync(loan);

            _loanRepositoryMock
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
            var result = await service.CancelLoanAsync(loan.BorrowerId, loan.Id, "Changed my mind");

            // Assert
            loan.Status.Should().Be(LoanStatus.Cancelled);
            loan.DecisionNote.Should().Be("Changed my mind");

            result.Status.Should().Be(LoanStatus.Cancelled);

            _loanRepositoryMock.Verify(r => r.Update(loan), Times.Once);
            _loanRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CancelLoanAsync_ShouldThrow_WhenUserIsNotBorrower()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateLoan(LoanStatus.Pending);

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(loan.Id))
                .ReturnsAsync(loan);

            // Act
            Func<Task> act = async () => await service.CancelLoanAsync("another-user", loan.Id, "No");

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You can only cancel your own loans.");
        }

        [Fact]
        public async Task DecideLoanAsync_ShouldApproveLoan_WhenOwnerOwnsItem()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateLoan(LoanStatus.Pending);

            var dto = new OwnerDecideLoanDto
            {
                IsApproved = true,
                DecisionNote = "Approved"
            };

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(loan.Id))
                .ReturnsAsync(loan);

            _loanRepositoryMock
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
            var result = await service.DecideLoanAsync(loan.LenderId, loan.Id, dto);

            // Assert
            loan.Status.Should().Be(LoanStatus.Approved);
            loan.OwnerApproverId.Should().Be(loan.LenderId);
            loan.DecisionNote.Should().Be("Approved");

            result.Status.Should().Be(LoanStatus.Approved);

            _loanRepositoryMock.Verify(r => r.Update(loan), Times.Once);
            _loanRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DecideLoanAsync_ShouldThrow_WhenUserIsNotOwner()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateLoan(LoanStatus.Pending);

            var dto = new OwnerDecideLoanDto
            {
                IsApproved = true
            };

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(loan.Id))
                .ReturnsAsync(loan);

            // Act
            Func<Task> act = async () => await service.DecideLoanAsync("another-user", loan.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You do not have permission to decide on this loan.");
        }

        [Fact]
        public async Task RequestExtensionAsync_ShouldThrow_WhenLoanIsNotActive()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateLoan(LoanStatus.Pending);

            var dto = new RequestExtensionDto
            {
                RequestedExtensionDate = loan.EndDate.AddDays(2)
            };

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(loan.Id))
                .ReturnsAsync(loan);

            // Act
            Func<Task> act = async () => await service.RequestExtensionAsync(loan.BorrowerId, loan.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Only active loans can be extended.");
        }

        [Fact]
        public async Task RequestExtensionAsync_ShouldRequestExtension_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateLoan(LoanStatus.Active);
            loan.Item.AvailableUntil = loan.EndDate.AddDays(10);

            var dto = new RequestExtensionDto
            {
                RequestedExtensionDate = loan.EndDate.AddDays(2)
            };

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(loan.Id))
                .ReturnsAsync(loan);

            _loanRepositoryMock
                .Setup(r => r.IsItemAvailableForDatesAsync(
                    loan.ItemId,
                    loan.EndDate.AddDays(1),
                    dto.RequestedExtensionDate.Date))
                .ReturnsAsync(true);

            _loanRepositoryMock
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
            var result = await service.RequestExtensionAsync(loan.BorrowerId, loan.Id, dto);

            // Assert
            loan.RequestedExtensionDate.Should().Be(dto.RequestedExtensionDate.Date);
            loan.ExtensionRequestStatus.Should().Be(ExtensionStatus.Pending);

            result.Id.Should().Be(loan.Id);

            _loanRepositoryMock.Verify(r => r.Update(loan), Times.Once);
            _loanRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ConfirmPickupAsync_ShouldSetLoanActive_WhenBorrowerScansQrCode()
        {
            // Arrange
            var service = CreateService();

            var item = CreateItem();
            item.QrCode = "QR123";

            var loan = CreateLoan(LoanStatus.Approved);
            loan.Item = item;
            loan.ItemId = item.Id;

            _itemRepositoryMock
                .Setup(r => r.GetByQrCodeAsync("QR123"))
                .ReturnsAsync(item);

            _loanRepositoryMock
                .Setup(r => r.GetActiveLoanByItemIdAsync(item.Id))
                .ReturnsAsync((Loan?)null);

            _loanRepositoryMock
                .Setup(r => r.GetByOwnerIdAsync(item.OwnerId))
                .ReturnsAsync(new List<Loan> { loan });

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(loan.Id))
                .ReturnsAsync(loan);

            _loanRepositoryMock
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
            var result = await service.ConfirmPickupAsync(loan.BorrowerId, new ScanQrCodeDto
            {
                QrCode = "QR123"
            });

            // Assert
            loan.Status.Should().Be(LoanStatus.Active);
            loan.PickedUpAt.Should().NotBeNull();
            item.Availability.Should().Be(ItemAvailability.OnRent);

            result.Status.Should().Be(LoanStatus.Active);

            _itemRepositoryMock.Verify(r => r.Update(item), Times.Once);
            _loanRepositoryMock.Verify(r => r.Update(loan), Times.Once);
            _loanRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ConfirmReturnAsync_ShouldCompleteLoan_WhenBorrowerScansQrCode()
        {
            // Arrange
            var service = CreateService();

            var item = CreateItem();
            item.QrCode = "QR123";

            var loan = CreateLoan(LoanStatus.Active);
            loan.Item = item;
            loan.ItemId = item.Id;
            loan.Borrower.Score = 90;
            loan.EndDate = DateTime.UtcNow.Date.AddDays(1);

            _itemRepositoryMock
                .Setup(r => r.GetByQrCodeAsync("QR123"))
                .ReturnsAsync(item);

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(loan.Id))
                .ReturnsAsync(loan);
                
            _loanRepositoryMock
                .Setup(r => r.GetActiveLoanByItemIdAsync(item.Id))
                .ReturnsAsync(loan);

            _scoreHistoryRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<ScoreHistory>()))
                .Returns(Task.CompletedTask);

            _loanRepositoryMock
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
            var result = await service.ConfirmReturnAsync(loan.BorrowerId, new ScanQrCodeDto
            {
                QrCode = "QR123"
            });

            // Assert
            loan.Status.Should().Be(LoanStatus.Completed);
            loan.ReturnedAt.Should().NotBeNull();
            loan.ActualReturnDate.Should().NotBeNull();
            loan.DisputeDeadline.Should().NotBeNull();

            item.Availability.Should().Be(ItemAvailability.Available);
            loan.Borrower.Score.Should().Be(95);

            result.Status.Should().Be(LoanStatus.Completed);

            _itemRepositoryMock.Verify(r => r.Update(item), Times.Once);
            _loanRepositoryMock.Verify(r => r.Update(loan), Times.Once);
            _loanRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task AdminReviewLoanAsync_ShouldApproveAdminPendingLoan()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateLoan(LoanStatus.AdminPending);
            var admin = CreateUser("admin-123", "Admin User");

            var dto = new AdminReviewLoanDto
            {
                IsApproved = true,
                AdminNote = "Approved by admin"
            };

            _userManagerMock
                .Setup(m => m.FindByIdAsync(admin.Id))
                .ReturnsAsync(admin);

            _userManagerMock
                .Setup(m => m.IsInRoleAsync(admin, "Admin"))
                .ReturnsAsync(true);

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(loan.Id))
                .ReturnsAsync(loan);

            _loanRepositoryMock
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
            var result = await service.AdminReviewLoanAsync("admin-123", loan.Id, dto);

            // Assert
            loan.Status.Should().Be(LoanStatus.Pending);
            loan.AdminReviewerId.Should().Be("admin-123");
            loan.DecisionNote.Should().Be(dto.AdminNote);

            result.Status.Should().Be(LoanStatus.Pending);

            _loanRepositoryMock.Verify(r => r.Update(loan), Times.Once);
            _loanRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrow_WhenUserHasNoAccess()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateLoan();

            var user = CreateUser("another-user", "Another User");

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(loan.Id))
                .ReturnsAsync(loan);

            _userManagerMock
                .Setup(m => m.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(m => m.IsInRoleAsync(user, "Admin"))
                .ReturnsAsync(false);

            // Act
            Func<Task> act = async () => await service.GetByIdAsync(loan.Id, user.Id);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You do not have access to this loan.");
        }

        [Fact]
        public async Task GetPendingAdminApprovalsCountAsync_ShouldReturnRepositoryCount()
        {
            // Arrange
            var service = CreateService();

            _loanRepositoryMock
                .Setup(r => r.GetPendingAdminApprovalsCountAsync())
                .ReturnsAsync(4);

            // Act
            var result = await service.GetPendingAdminApprovalsCountAsync();

            // Assert
            result.Should().Be(4);
        }

        [Fact]
        public async Task GetActiveLoansCountAsync_ShouldReturnRepositoryCount()
        {
            // Arrange
            var service = CreateService();

            _loanRepositoryMock
                .Setup(r => r.GetActiveLoansCountAsync())
                .ReturnsAsync(6);

            // Act
            var result = await service.GetActiveLoansCountAsync();

            // Assert
            result.Should().Be(6);
        }
    }
}