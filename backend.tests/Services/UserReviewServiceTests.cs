using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace backend.Tests.Services
{
    public class UserReviewServiceTests
    {
        private readonly Mock<IUserReviewRepository> _reviewRepositoryMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<ILoanRepository> _loanRepositoryMock = new();

        private UserReviewService CreateService()
        {
            return new UserReviewService(
                _reviewRepositoryMock.Object,
                _userRepositoryMock.Object,
                _loanRepositoryMock.Object
            );
        }

        private static ApplicationUser CreateUser(string id, string name)
        {
            return new ApplicationUser
            {
                Id = id,
                FullName = name,
                UserName = name.Replace(" ", "").ToLower(),
                AvatarUrl = $"{id}.png"
            };
        }

        private static Loan CreateCompletedLoan()
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
                Status = LoanStatus.Completed,
                Item = new Item
                {
                    Id = 10,
                    Title = "Hammer"
                }
            };
        }

        private static UserReview CreateReview()
        {
            var reviewer = CreateUser("borrower-123", "Borrower User");
            var reviewed = CreateUser("lender-123", "Lender User");

            return new UserReview
            {
                Id = 1,
                LoanId = 1,
                ReviewerId = reviewer.Id,
                Reviewer = reviewer,
                ReviewedUserId = reviewed.Id,
                ReviewedUser = reviewed,
                Rating = 5,
                Comment = "Good user",
                IsAdminReview = false,
                CreatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task CreateReviewAsync_ShouldThrow_WhenLoanDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var dto = new CreateUserReviewDto
            {
                LoanId = 1,
                Rating = 5,
                Comment = "Good user"
            };

            _loanRepositoryMock
                .Setup(r => r.GetByIdAsync(dto.LoanId))
                .ReturnsAsync((Loan?)null);

            // Act
            Func<Task> act = async () =>
                await service.CreateReviewAsync("borrower-123", dto);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Loan not found.");

            _reviewRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserReview>()), Times.Never);
        }

        [Fact]
        public async Task CreateReviewAsync_ShouldThrow_WhenLoanIsNotCompleted()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateCompletedLoan();
            loan.Status = LoanStatus.Active;

            var dto = new CreateUserReviewDto
            {
                LoanId = loan.Id,
                Rating = 5,
                Comment = "Good user"
            };

            _loanRepositoryMock
                .Setup(r => r.GetByIdAsync(loan.Id))
                .ReturnsAsync(loan);

            // Act
            Func<Task> act = async () =>
                await service.CreateReviewAsync(loan.BorrowerId, dto);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("You can only leave a review after the loan is completed.");

            _reviewRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserReview>()), Times.Never);
        }

        [Fact]
        public async Task CreateReviewAsync_ShouldThrow_WhenReviewerWasNotPartOfLoan()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateCompletedLoan();

            var dto = new CreateUserReviewDto
            {
                LoanId = loan.Id,
                Rating = 5,
                Comment = "Good user"
            };

            _loanRepositoryMock
                .Setup(r => r.GetByIdAsync(loan.Id))
                .ReturnsAsync(loan);

            // Act
            Func<Task> act = async () =>
                await service.CreateReviewAsync("stranger-user", dto);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You were not part of this loan.");

            _reviewRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserReview>()), Times.Never);
        }

        [Fact]
        public async Task CreateReviewAsync_ShouldThrow_WhenUserAlreadyReviewedOtherParty()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateCompletedLoan();

            var dto = new CreateUserReviewDto
            {
                LoanId = loan.Id,
                Rating = 5,
                Comment = "Good user"
            };

            _loanRepositoryMock
                .Setup(r => r.GetByIdAsync(loan.Id))
                .ReturnsAsync(loan);

            _reviewRepositoryMock
                .Setup(r => r.HasReviewedUserAsync(loan.BorrowerId, loan.LenderId))
                .ReturnsAsync(true);

            // Act
            Func<Task> act = async () =>
                await service.CreateReviewAsync(loan.BorrowerId, dto);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("You have already reviewed this user.");

            _reviewRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserReview>()), Times.Never);
        }

        [Fact]
        public async Task CreateReviewAsync_ShouldCreateReview_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var loan = CreateCompletedLoan();

            var dto = new CreateUserReviewDto
            {
                LoanId = loan.Id,
                Rating = 5,
                Comment = " Great lender "
            };

            _loanRepositoryMock
                .Setup(r => r.GetByIdAsync(loan.Id))
                .ReturnsAsync(loan);

            _reviewRepositoryMock
                .Setup(r => r.HasReviewedUserAsync(loan.BorrowerId, loan.LenderId))
                .ReturnsAsync(false);

            _reviewRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<UserReview>()))
                .Callback<UserReview>(review =>
                {
                    review.Id = 1;
                    review.Reviewer = loan.Borrower;
                    review.ReviewedUser = loan.Lender;
                })
                .Returns(Task.CompletedTask);

            _reviewRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _reviewRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(1))
                .ReturnsAsync(new UserReview
                {
                    Id = 1,
                    LoanId = loan.Id,
                    ReviewerId = loan.BorrowerId,
                    Reviewer = loan.Borrower,
                    ReviewedUserId = loan.LenderId,
                    ReviewedUser = loan.Lender,
                    Rating = 5,
                    Comment = "Great lender",
                    IsAdminReview = false,
                    CreatedAt = DateTime.UtcNow
                });

            // Act
            var result = await service.CreateReviewAsync(loan.BorrowerId, dto);

            // Assert
            result.Id.Should().Be(1);
            result.LoanId.Should().Be(loan.Id);
            result.ReviewerId.Should().Be(loan.BorrowerId);
            result.ReviewedUserId.Should().Be(loan.LenderId);
            result.Rating.Should().Be(5);
            result.Comment.Should().Be("Great lender");
            result.IsMine.Should().BeTrue();

            _reviewRepositoryMock.Verify(r => r.AddAsync(It.Is<UserReview>(review =>
                review.LoanId == loan.Id &&
                review.ReviewerId == loan.BorrowerId &&
                review.ReviewedUserId == loan.LenderId &&
                review.Rating == 5 &&
                review.Comment == "Great lender" &&
                review.IsAdminReview == false
            )), Times.Once);

            _reviewRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateReviewAsync_ShouldThrow_WhenReviewDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var dto = new UpdateUserReviewDto
            {
                Rating = 4,
                Comment = "Updated"
            };

            _reviewRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(1))
                .ReturnsAsync((UserReview?)null);

            // Act
            Func<Task> act = async () =>
                await service.UpdateReviewAsync(1, "user-123", dto);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Review not found.");
        }

        [Fact]
        public async Task UpdateReviewAsync_ShouldThrow_WhenUserDoesNotOwnReview()
        {
            // Arrange
            var service = CreateService();

            var review = CreateReview();

            var dto = new UpdateUserReviewDto
            {
                Rating = 4,
                Comment = "Updated"
            };

            _reviewRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(review.Id))
                .ReturnsAsync(review);

            // Act
            Func<Task> act = async () =>
                await service.UpdateReviewAsync(review.Id, "another-user", dto);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You can only edit your own reviews.");
        }

        [Fact]
        public async Task UpdateReviewAsync_ShouldUpdateReview_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var review = CreateReview();

            var dto = new UpdateUserReviewDto
            {
                Rating = 4,
                Comment = " Updated comment "
            };

            _reviewRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(review.Id))
                .ReturnsAsync(review);

            _reviewRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await service.UpdateReviewAsync(review.Id, review.ReviewerId, dto);

            // Assert
            review.Rating.Should().Be(4);
            review.Comment.Should().Be("Updated comment");
            review.IsEdited.Should().BeTrue();
            review.EditedAt.Should().NotBeNull();

            result.Rating.Should().Be(4);
            result.Comment.Should().Be("Updated comment");
            result.IsMine.Should().BeTrue();

            _reviewRepositoryMock.Verify(r => r.Update(review), Times.Once);
            _reviewRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrow_WhenReviewDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            _reviewRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(99))
                .ReturnsAsync((UserReview?)null);

            // Act
            Func<Task> act = async () =>
                await service.GetByIdAsync(99, "user-123", isAdmin: false);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Review not found.");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnReview_WhenExists()
        {
            // Arrange
            var service = CreateService();

            var review = CreateReview();

            _reviewRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(review.Id))
                .ReturnsAsync(review);

            // Act
            var result = await service.GetByIdAsync(review.Id, review.ReviewerId, isAdmin: false);

            // Assert
            result.Id.Should().Be(review.Id);
            result.ReviewerId.Should().Be(review.ReviewerId);
            result.ReviewedUserId.Should().Be(review.ReviewedUserId);
            result.IsMine.Should().BeTrue();
        }

        [Fact]
        public async Task AdminCreateReviewAsync_ShouldThrow_WhenReviewedUserDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var dto = new AdminCreateUserReviewDto
            {
                ReviewedUserId = "missing-user",
                Rating = 5,
                Comment = "Admin review"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(dto.ReviewedUserId))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            Func<Task> act = async () =>
                await service.AdminCreateReviewAsync("admin-123", dto);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Reviewed user not found.");

            _reviewRepositoryMock.Verify(r => r.AddAsync(It.IsAny<UserReview>()), Times.Never);
        }

        [Fact]
        public async Task AdminCreateReviewAsync_ShouldCreateAdminReview_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var reviewedUser = CreateUser("reviewed-123", "Reviewed User");
            var admin = CreateUser("admin-123", "Admin User");

            var dto = new AdminCreateUserReviewDto
            {
                ReviewedUserId = reviewedUser.Id,
                Rating = 5,
                Comment = " Admin review "
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(reviewedUser.Id))
                .ReturnsAsync(reviewedUser);

            _reviewRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<UserReview>()))
                .Callback<UserReview>(review =>
                {
                    review.Id = 10;
                    review.Reviewer = admin;
                    review.ReviewedUser = reviewedUser;
                })
                .Returns(Task.CompletedTask);

            _reviewRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _reviewRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(10))
                .ReturnsAsync(new UserReview
                {
                    Id = 10,
                    LoanId = null,
                    ReviewerId = admin.Id,
                    Reviewer = admin,
                    ReviewedUserId = reviewedUser.Id,
                    ReviewedUser = reviewedUser,
                    Rating = 5,
                    Comment = "Admin review",
                    IsAdminReview = true,
                    CreatedAt = DateTime.UtcNow
                });

            // Act
            var result = await service.AdminCreateReviewAsync(admin.Id, dto);

            // Assert
            result.Id.Should().Be(10);
            result.ReviewerId.Should().Be(admin.Id);
            result.ReviewedUserId.Should().Be(reviewedUser.Id);
            result.IsAdminReview.Should().BeTrue();
            result.Comment.Should().Be("Admin review");

            _reviewRepositoryMock.Verify(r => r.AddAsync(It.Is<UserReview>(review =>
                review.LoanId == null &&
                review.ReviewerId == admin.Id &&
                review.ReviewedUserId == reviewedUser.Id &&
                review.Rating == 5 &&
                review.Comment == "Admin review" &&
                review.IsAdminReview
            )), Times.Once);

            _reviewRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task AdminDeleteReviewAsync_ShouldThrow_WhenReviewDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            _reviewRepositoryMock
                .Setup(r => r.GetByIdAsync(99))
                .ReturnsAsync((UserReview?)null);

            // Act
            Func<Task> act = async () =>
                await service.AdminDeleteReviewAsync(99);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Review not found.");
        }

        [Fact]
        public async Task AdminDeleteReviewAsync_ShouldSoftDeleteReview_WhenExists()
        {
            // Arrange
            var service = CreateService();

            var review = CreateReview();

            _reviewRepositoryMock
                .Setup(r => r.GetByIdAsync(review.Id))
                .ReturnsAsync(review);

            _reviewRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await service.AdminDeleteReviewAsync(review.Id);

            // Assert
            review.IsDeleted.Should().BeTrue();
            review.DeletedAt.Should().NotBeNull();

            _reviewRepositoryMock.Verify(r => r.Update(review), Times.Once);
            _reviewRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetReviewsForUserAsync_ShouldReturnPagedReviews()
        {
            // Arrange
            var service = CreateService();

            var review = CreateReview();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            _reviewRepositoryMock
                .Setup(r => r.GetByReviewedUserIdAsync(review.ReviewedUserId, null, request))
                .ReturnsAsync(new PagedResult<UserReview>
                {
                    Items = new List<UserReview> { review },
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 10
                });

            // Act
            var result = await service.GetReviewsForUserAsync(review.ReviewedUserId, null, request);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items[0].Id.Should().Be(review.Id);
            result.Items[0].IsMine.Should().BeFalse();
            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetMyGivenReviewsAsync_ShouldReturnPagedReviews()
        {
            // Arrange
            var service = CreateService();

            var review = CreateReview();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            _reviewRepositoryMock
                .Setup(r => r.GetByReviewerIdAsync(review.ReviewerId, null, request))
                .ReturnsAsync(new PagedResult<UserReview>
                {
                    Items = new List<UserReview> { review },
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 10
                });

            // Act
            var result = await service.GetMyGivenReviewsAsync(review.ReviewerId, null, request);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items[0].Id.Should().Be(review.Id);
            result.Items[0].IsMine.Should().BeTrue();
            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetRatingSummaryAsync_ShouldReturnRepositoryResult()
        {
            // Arrange
            var service = CreateService();

            var summary = new UserRatingSummaryDto
            {
                AverageRating = 4.5,
                TotalReviews = 2
            };

            _reviewRepositoryMock
                .Setup(r => r.GetRatingSummaryAsync("user-123"))
                .ReturnsAsync(summary);

            // Act
            var result = await service.GetRatingSummaryAsync("user-123");

            // Assert
            result.Should().BeSameAs(summary);
            result.AverageRating.Should().Be(4.5);
            result.TotalReviews.Should().Be(2);
        }

     
    }
}