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
    public class ItemReviewServiceTests
    {
        private readonly Mock<IItemReviewRepository> _itemReviewRepositoryMock = new();
        private readonly Mock<IItemRepository> _itemRepositoryMock = new();
        private readonly Mock<ILoanRepository> _loanRepositoryMock = new();
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

        public ItemReviewServiceTests()
        {
            _userManagerMock = MockUserManager();
        }

        private ItemReviewService CreateService()
        {
            return new ItemReviewService(
                _itemReviewRepositoryMock.Object,
                _itemRepositoryMock.Object,
                _userManagerMock.Object,
                _loanRepositoryMock.Object
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
            string id = "user-123",
            string username = "testuser")
        {
            return new ApplicationUser
            {
                Id = id,
                UserName = username,
                FullName = "Test User",
                AvatarUrl = "avatar.png"
            };
        }

        private static Loan CreateCompletedLoan()
        {
            var borrower = CreateUser("borrower-123", "borrower");

            return new Loan
            {
                Id = 1,
                ItemId = 10,
                BorrowerId = borrower.Id,
                Borrower = borrower,
                Status = LoanStatus.Completed,
                Item = new Item
                {
                    Id = 10,
                    Title = "Test Item"
                }
            };
        }

        private static ItemReview CreateReview()
        {
            var reviewer = CreateUser();

            return new ItemReview
            {
                Id = 1,
                ItemId = 10,
                LoanId = 1,
                ReviewerId = reviewer.Id,
                Reviewer = reviewer,
                Rating = 4,
                Comment = "Good item",
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        [Fact]
        public async Task CreateItemReviewAsync_ShouldThrow_WhenUserReviewHasNoLoanId()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser();

            var dto = new CreateItemReviewDto
            {
                ItemId = 10,
                LoanId = null,
                Rating = 5,
                Comment = "Good item"
            };

            _userManagerMock
                .Setup(m => m.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(m => m.IsInRoleAsync(user, "Admin"))
                .ReturnsAsync(false);

            // Act
            Func<Task> act = async () => await service.CreateItemReviewAsync(user.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("LoanId is required for user reviews.");

            _itemReviewRepositoryMock.Verify(r => r.AddItemReviewAsync(It.IsAny<ItemReview>()), Times.Never);
        }

        [Fact]
        public async Task CreateItemReviewAsync_ShouldThrow_WhenLoanDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser("borrower-123", "borrower");

            var dto = new CreateItemReviewDto
            {
                LoanId = 1,
                Rating = 5,
                Comment = "Good item"
            };

            _userManagerMock
                .Setup(m => m.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(m => m.IsInRoleAsync(user, "Admin"))
                .ReturnsAsync(false);

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(dto.LoanId.Value))
                .ReturnsAsync((Loan?)null);

            // Act
            Func<Task> act = async () => await service.CreateItemReviewAsync(user.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Loan 1 not found.");
        }

        [Fact]
        public async Task CreateItemReviewAsync_ShouldThrow_WhenReviewerIsNotBorrower()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateCompletedLoan();

            var dto = new CreateItemReviewDto
            {
                LoanId = loan.Id,
                Rating = 5,
                Comment = "Good item"
            };

            var user = CreateUser("another-user", "another");

            _userManagerMock
                .Setup(m => m.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(m => m.IsInRoleAsync(user, "Admin"))
                .ReturnsAsync(false);

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(loan.Id))
                .ReturnsAsync(loan);

            // Act
            Func<Task> act = async () => await service.CreateItemReviewAsync(user.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Only the borrower of this loan can review the item.");
        }

        [Fact]
        public async Task CreateItemReviewAsync_ShouldThrow_WhenLoanIsNotCompleted()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateCompletedLoan();
            loan.Status = LoanStatus.Active;

            var user = loan.Borrower!;

            var dto = new CreateItemReviewDto
            {
                LoanId = loan.Id,
                Rating = 5,
                Comment = "Good item"
            };

            _userManagerMock
                .Setup(m => m.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(m => m.IsInRoleAsync(user, "Admin"))
                .ReturnsAsync(false);

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(loan.Id))
                .ReturnsAsync(loan);

            // Act
            Func<Task> act = async () => await service.CreateItemReviewAsync(user.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("You can only review an item after the loan is completed.");
        }

        [Fact]
        public async Task CreateItemReviewAsync_ShouldCreateReview_WhenValidUserReview()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateCompletedLoan();
            var user = loan.Borrower!;

            var item = new Item
            {
                Id = loan.ItemId,
                Title = "Test Item"
            };

            var dto = new CreateItemReviewDto
            {
                LoanId = loan.Id,
                Rating = 5,
                Comment = " Great item "
            };

            _userManagerMock
                .Setup(m => m.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(m => m.IsInRoleAsync(user, "Admin"))
                .ReturnsAsync(false);

            _loanRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(loan.Id))
                .ReturnsAsync(loan);

            _itemReviewRepositoryMock
                .Setup(r => r.GetItemReviewByLoanIdAsync(loan.Id))
                .ReturnsAsync((ItemReview?)null);

            _itemReviewRepositoryMock
                .Setup(r => r.AddItemReviewAsync(It.IsAny<ItemReview>()))
                .Callback<ItemReview>(review =>
                {
                    review.Id = 1;
                    review.Reviewer = user;
                })
                .Returns(Task.CompletedTask);

            _itemReviewRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _itemReviewRepositoryMock
                .Setup(r => r.LoadReviewerAsync(It.IsAny<ItemReview>()))
                .Returns(Task.CompletedTask);

            _itemRepositoryMock
                .Setup(r => r.GetByIdAsync(item.Id))
                .ReturnsAsync(item);

            _itemReviewRepositoryMock
                .Setup(r => r.GetRatingsByItemIdAsync(item.Id))
                .ReturnsAsync(new List<int> { 5, 3 });

            _itemRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await service.CreateItemReviewAsync(user.Id, dto);

            // Assert
            result.Id.Should().Be(1);
            result.ItemId.Should().Be(loan.ItemId);
            result.LoanId.Should().Be(loan.Id);
            result.Rating.Should().Be(5);
            result.Comment.Should().Be("Great item");
            result.IsMine.Should().BeTrue();

            item.AverageRating.Should().Be(4);

            _itemReviewRepositoryMock.Verify(r => r.AddItemReviewAsync(It.Is<ItemReview>(review =>
                review.ItemId == loan.ItemId &&
                review.LoanId == loan.Id &&
                review.ReviewerId == user.Id &&
                review.Rating == 5 &&
                review.Comment == "Great item" &&
                review.IsAdminReview == false
            )), Times.Once);

            _itemRepositoryMock.Verify(r => r.Update(item), Times.Once);
            _itemRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task EditItemReviewAsync_ShouldThrow_WhenReviewDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var dto = new UpdateItemReviewDto
            {
                Rating = 4,
                Comment = "Updated"
            };

            _itemReviewRepositoryMock
                .Setup(r => r.GetItemReviewByIdAsync(1))
                .ReturnsAsync((ItemReview?)null);

            // Act
            Func<Task> act = async () => await service.EditItemReviewAsync(1, "user-123", dto);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Review not found.");
        }

        [Fact]
        public async Task EditItemReviewAsync_ShouldThrow_WhenUserDoesNotOwnReview()
        {
            // Arrange
            var service = CreateService();

            var review = CreateReview();

            var dto = new UpdateItemReviewDto
            {
                Rating = 4,
                Comment = "Updated"
            };

            _itemReviewRepositoryMock
                .Setup(r => r.GetItemReviewByIdAsync(review.Id))
                .ReturnsAsync(review);

            // Act
            Func<Task> act = async () =>
                await service.EditItemReviewAsync(review.Id, "another-user", dto);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You can only edit your own reviews.");
        }

        [Fact]
        public async Task EditItemReviewAsync_ShouldUpdateReview_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var review = CreateReview();

            var item = new Item
            {
                Id = review.ItemId,
                Title = "Test Item"
            };

            var dto = new UpdateItemReviewDto
            {
                Rating = 5,
                Comment = " Updated comment "
            };

            _itemReviewRepositoryMock
                .Setup(r => r.GetItemReviewByIdAsync(review.Id))
                .ReturnsAsync(review);

            _itemReviewRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _itemRepositoryMock
                .Setup(r => r.GetByIdAsync(item.Id))
                .ReturnsAsync(item);

            _itemReviewRepositoryMock
                .Setup(r => r.GetRatingsByItemIdAsync(item.Id))
                .ReturnsAsync(new List<int> { 5, 5 });

            _itemRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await service.EditItemReviewAsync(review.Id, review.ReviewerId, dto);

            // Assert
            review.Rating.Should().Be(5);
            review.Comment.Should().Be("Updated comment");
            review.IsEdited.Should().BeTrue();
            review.EditedAt.Should().NotBeNull();

            result.Rating.Should().Be(5);
            result.Comment.Should().Be("Updated comment");
            result.IsMine.Should().BeTrue();

            item.AverageRating.Should().Be(5);

            _itemReviewRepositoryMock.Verify(r => r.Update(review), Times.Once);
            _itemReviewRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteItemReviewAsync_ShouldThrow_WhenUserIsNotAdmin()
        {
            // Arrange
            var service = CreateService();

            var review = CreateReview();
            var user = CreateUser("user-123", "normaluser");

            _itemReviewRepositoryMock
                .Setup(r => r.GetItemReviewByIdAsync(review.Id))
                .ReturnsAsync(review);

            _userManagerMock
                .Setup(m => m.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(m => m.IsInRoleAsync(user, "Admin"))
                .ReturnsAsync(false);

            // Act
            Func<Task> act = async () =>
                await service.DeleteItemReviewAsync(review.Id, user.Id);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Only admins can delete reviews.");
        }

        [Fact]
        public async Task DeleteItemReviewAsync_ShouldDeleteReview_WhenUserIsAdmin()
        {
            // Arrange
            var service = CreateService();

            var review = CreateReview();

            var admin = CreateUser("admin-123", "admin");

            var item = new Item
            {
                Id = review.ItemId,
                Title = "Test Item"
            };

            _itemReviewRepositoryMock
                .Setup(r => r.GetItemReviewByIdAsync(review.Id))
                .ReturnsAsync(review);

            _userManagerMock
                .Setup(m => m.FindByIdAsync(admin.Id))
                .ReturnsAsync(admin);

            _userManagerMock
                .Setup(m => m.IsInRoleAsync(admin, "Admin"))
                .ReturnsAsync(true);

            _itemReviewRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _itemRepositoryMock
                .Setup(r => r.GetByIdAsync(item.Id))
                .ReturnsAsync(item);

            _itemReviewRepositoryMock
                .Setup(r => r.GetRatingsByItemIdAsync(item.Id))
                .ReturnsAsync(new List<int>());

            _itemRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.DeleteItemReviewAsync(review.Id, admin.Id);

            // Assert
            review.IsDeleted.Should().BeTrue();
            review.DeletedByAdminId.Should().Be(admin.Id);
            review.DeletedAt.Should().NotBeNull();

            item.AverageRating.Should().BeNull();

            _itemReviewRepositoryMock.Verify(r => r.Update(review), Times.Once);
            _itemReviewRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByItemIdAsync_ShouldReturnPagedReviews()
        {
            // Arrange
            var service = CreateService();

            var review = CreateReview();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            _itemReviewRepositoryMock
                .Setup(r => r.GetItemReviewsByItemIdAsync(review.ItemId, null, request))
                .ReturnsAsync(new PagedResult<ItemReview>
                {
                    Items = new List<ItemReview> { review },
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 10
                });

            // Act
            var result = await service.GetByItemIdAsync(review.ItemId, review.ReviewerId, null, request);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items[0].Id.Should().Be(review.Id);
            result.Items[0].IsMine.Should().BeTrue();
            result.TotalCount.Should().Be(1);
        }
    }
}