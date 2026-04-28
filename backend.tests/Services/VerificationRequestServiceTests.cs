using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace backend.Tests.Services
{
    public class VerificationRequestServiceTests
    {
        private readonly Mock<IVerificationRequestRepository> _verificationRepositoryMock = new();
        private readonly Mock<IUserRepository> _userRepositoryMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();

        private VerificationRequestService CreateService()
        {
            return new VerificationRequestService(
                _verificationRepositoryMock.Object,
                _userRepositoryMock.Object,
                _notificationServiceMock.Object
            );
        }

        private static ApplicationUser CreateUser(string id = "user-123", bool isVerified = false)
        {
            return new ApplicationUser
            {
                Id = id,
                FullName = "Test User",
                UserName = "testuser",
                AvatarUrl = "avatar.png",
                IsVerified = isVerified
            };
        }

        private static ApplicationUser CreateAdmin(string id = "admin-123")
        {
            return new ApplicationUser
            {
                Id = id,
                FullName = "Admin User",
                UserName = "adminuser",
                AvatarUrl = "admin.png"
            };
        }

        private static VerificationRequest CreateVerificationRequest(
            int id = 1,
            string userId = "user-123",
            VerificationStatus status = VerificationStatus.Pending)
        {
            var user = CreateUser(userId);

            return new VerificationRequest
            {
                Id = id,
                UserId = userId,
                User = user,
                DocumentType = 0,
                DocumentUrl = "https://example.com/passport.png",
                Status = status,
                SubmittedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task SubmitRequestAsync_ShouldThrow_WhenUserDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var dto = new CreateVerificationRequestDto
            {
                DocumentType = 0,
                DocumentUrl = "https://example.com/passport.png"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync("missing-user"))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            Func<Task> act = async () =>
                await service.SubmitRequestAsync("missing-user", dto);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("User not found.");

            _verificationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<VerificationRequest>()), Times.Never);
        }

        [Fact]
        public async Task SubmitRequestAsync_ShouldThrow_WhenUserIsAlreadyVerified()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser(isVerified: true);

            var dto = new CreateVerificationRequestDto
            {
                DocumentType = 0,
                DocumentUrl = "https://example.com/passport.png"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            // Act
            Func<Task> act = async () =>
                await service.SubmitRequestAsync(user.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("Your account is already verified.");

            _verificationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<VerificationRequest>()), Times.Never);
        }

        [Fact]
        public async Task SubmitRequestAsync_ShouldThrow_WhenUserAlreadyHasPendingRequest()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser();

            var dto = new CreateVerificationRequestDto
            {
                DocumentType = 0,
                DocumentUrl = "https://example.com/passport.png"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _verificationRepositoryMock
                .Setup(r => r.HasPendingRequestAsync(user.Id))
                .ReturnsAsync(true);

            // Act
            Func<Task> act = async () =>
                await service.SubmitRequestAsync(user.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("You already have a pending verification request.");

            _verificationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<VerificationRequest>()), Times.Never);
        }

        [Fact]
        public async Task SubmitRequestAsync_ShouldCreateVerificationRequest_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser();

            var dto = new CreateVerificationRequestDto
            {
                DocumentType = 0,
                DocumentUrl = " https://example.com/passport.png "
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _verificationRepositoryMock
                .Setup(r => r.HasPendingRequestAsync(user.Id))
                .ReturnsAsync(false);

            _verificationRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<VerificationRequest>()))
                .Callback<VerificationRequest>(request =>
                {
                    request.Id = 1;
                    request.User = user;
                })
                .Returns(Task.CompletedTask);

            _verificationRepositoryMock
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

            _verificationRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(1))
                .ReturnsAsync(new VerificationRequest
                {
                    Id = 1,
                    UserId = user.Id,
                    User = user,
                    DocumentType = dto.DocumentType,
                    DocumentUrl = "https://example.com/passport.png",
                    Status = VerificationStatus.Pending,
                    SubmittedAt = DateTime.UtcNow
                });

            // Act
            var result = await service.SubmitRequestAsync(user.Id, dto);

            // Assert
            result.Id.Should().Be(1);
            result.UserId.Should().Be(user.Id);
            result.DocumentType.Should().Be(0);
            result.DocumentUrl.Should().Be("https://example.com/passport.png");
            result.Status.Should().Be(VerificationStatus.Pending);

            _verificationRepositoryMock.Verify(r => r.AddAsync(It.Is<VerificationRequest>(request =>
                request.UserId == user.Id &&
                request.DocumentType == dto.DocumentType &&
                request.DocumentUrl == "https://example.com/passport.png" &&
                request.Status == VerificationStatus.Pending
            )), Times.Once);

            _verificationRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);

            _notificationServiceMock.Verify(n => n.SendAsync(
                user.Id,
                NotificationType.VerificationSubmitted,
                It.IsAny<string>(),
                1,
                NotificationReferenceType.Verification
            ), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrow_WhenRequestDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            _verificationRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(99))
                .ReturnsAsync((VerificationRequest?)null);

            // Act
            Func<Task> act = async () =>
                await service.GetByIdAsync(99, "user-123", isAdmin: false);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Verification request not found.");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldThrow_WhenUserDoesNotOwnRequest()
        {
            // Arrange
            var service = CreateService();

            var request = CreateVerificationRequest(userId: "owner-user");

            _verificationRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(request.Id))
                .ReturnsAsync(request);

            // Act
            Func<Task> act = async () =>
                await service.GetByIdAsync(request.Id, "another-user", isAdmin: false);

            // Assert
            await act.Should()
                .ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You cannot view this verification request.");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnRequest_WhenUserOwnsRequest()
        {
            // Arrange
            var service = CreateService();

            var request = CreateVerificationRequest();

            _verificationRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(request.Id))
                .ReturnsAsync(request);

            // Act
            var result = await service.GetByIdAsync(request.Id, request.UserId, isAdmin: false);

            // Assert
            result.Id.Should().Be(request.Id);
            result.UserId.Should().Be(request.UserId);
            result.Status.Should().Be(request.Status);
        }

        [Fact]
        public async Task DecideAsync_ShouldThrow_WhenRequestDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var dto = new AdminDecideVerificationRequestDto
            {
                Status = VerificationStatus.Approved,
                AdminNote = "Approved"
            };

            _verificationRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(99))
                .ReturnsAsync((VerificationRequest?)null);

            // Act
            Func<Task> act = async () =>
                await service.DecideAsync(99, "admin-123", dto);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Verification request not found.");
        }

        [Fact]
        public async Task DecideAsync_ShouldThrow_WhenAdminDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var request = CreateVerificationRequest();

            var dto = new AdminDecideVerificationRequestDto
            {
                Status = VerificationStatus.Approved,
                AdminNote = "Approved"
            };

            _verificationRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(request.Id))
                .ReturnsAsync(request);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync("missing-admin"))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            Func<Task> act = async () =>
                await service.DecideAsync(request.Id, "missing-admin", dto);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("Admin not found.");
        }

        [Fact]
        public async Task DecideAsync_ShouldThrow_WhenRequestAlreadyReviewed()
        {
            // Arrange
            var service = CreateService();

            var request = CreateVerificationRequest(status: VerificationStatus.Approved);
            var admin = CreateAdmin();

            var dto = new AdminDecideVerificationRequestDto
            {
                Status = VerificationStatus.Approved,
                AdminNote = "Approved"
            };

            _verificationRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(request.Id))
                .ReturnsAsync(request);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(admin.Id))
                .ReturnsAsync(admin);

            // Act
            Func<Task> act = async () =>
                await service.DecideAsync(request.Id, admin.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<InvalidOperationException>()
                .WithMessage("This request has already been reviewed.");
        }

        [Fact]
        public async Task DecideAsync_ShouldThrow_WhenDecisionStatusIsPending()
        {
            // Arrange
            var service = CreateService();

            var request = CreateVerificationRequest();
            var admin = CreateAdmin();

            var dto = new AdminDecideVerificationRequestDto
            {
                Status = VerificationStatus.Pending,
                AdminNote = "Still pending"
            };

            _verificationRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(request.Id))
                .ReturnsAsync(request);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(admin.Id))
                .ReturnsAsync(admin);

            // Act
            Func<Task> act = async () =>
                await service.DecideAsync(request.Id, admin.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("Decision status cannot be Pending.");
        }

        [Fact]
        public async Task DecideAsync_ShouldThrow_WhenRejectedWithoutReason()
        {
            // Arrange
            var service = CreateService();

            var request = CreateVerificationRequest();
            var admin = CreateAdmin();

            var dto = new AdminDecideVerificationRequestDto
            {
                Status = VerificationStatus.Rejected,
                AdminNote = "   "
            };

            _verificationRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(request.Id))
                .ReturnsAsync(request);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(admin.Id))
                .ReturnsAsync(admin);

            // Act
            Func<Task> act = async () =>
                await service.DecideAsync(request.Id, admin.Id, dto);

            // Assert
            await act.Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("A reason is required when rejecting a verification request.");
        }

        [Fact]
        public async Task DecideAsync_ShouldApproveRequestAndVerifyUser_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var request = CreateVerificationRequest();
            var admin = CreateAdmin();

            var dto = new AdminDecideVerificationRequestDto
            {
                Status = VerificationStatus.Approved,
                AdminNote = " Looks good "
            };

            _verificationRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(request.Id))
                .ReturnsAsync(request);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(admin.Id))
                .ReturnsAsync(admin);

            _userRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _verificationRepositoryMock
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
            var result = await service.DecideAsync(request.Id, admin.Id, dto);

            // Assert
            request.Status.Should().Be(VerificationStatus.Approved);
            request.ReviewedByAdminId.Should().Be(admin.Id);
            request.ReviewedByAdmin.Should().Be(admin);
            request.AdminNote.Should().Be("Looks good");
            request.ReviewedAt.Should().NotBeNull();
            request.User!.IsVerified.Should().BeTrue();

            result.Status.Should().Be(VerificationStatus.Approved);
            result.AdminNote.Should().Be("Looks good");

            _userRepositoryMock.Verify(r => r.Update(request.User), Times.Once);
            _verificationRepositoryMock.Verify(r => r.Update(request), Times.Once);
            _userRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
            _verificationRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DecideAsync_ShouldRejectRequest_WhenValid()
        {
            // Arrange
            var service = CreateService();

            var request = CreateVerificationRequest();
            var admin = CreateAdmin();

            var dto = new AdminDecideVerificationRequestDto
            {
                Status = VerificationStatus.Rejected,
                AdminNote = " Document is unclear "
            };

            _verificationRepositoryMock
                .Setup(r => r.GetByIdWithDetailsAsync(request.Id))
                .ReturnsAsync(request);

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(admin.Id))
                .ReturnsAsync(admin);

            _userRepositoryMock
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            _verificationRepositoryMock
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
            var result = await service.DecideAsync(request.Id, admin.Id, dto);

            // Assert
            request.Status.Should().Be(VerificationStatus.Rejected);
            request.ReviewedByAdminId.Should().Be(admin.Id);
            request.AdminNote.Should().Be("Document is unclear");
            request.User!.IsVerified.Should().BeFalse();

            result.Status.Should().Be(VerificationStatus.Rejected);
            result.AdminNote.Should().Be("Document is unclear");

            _userRepositoryMock.Verify(r => r.Update(It.IsAny<ApplicationUser>()), Times.Never);
            _verificationRepositoryMock.Verify(r => r.Update(request), Times.Once);
            _userRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
            _verificationRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetMyRequestsAsync_ShouldReturnPagedRequests()
        {
            // Arrange
            var service = CreateService();

            var request = CreateVerificationRequest();

            var pagedRequest = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            _verificationRepositoryMock
                .Setup(r => r.GetByUserIdAsync(request.UserId, null, pagedRequest))
                .ReturnsAsync(new PagedResult<VerificationRequest>
                {
                    Items = new List<VerificationRequest> { request },
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 10
                });

            // Act
            var result = await service.GetMyRequestsAsync(request.UserId, null, pagedRequest);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items[0].Id.Should().Be(request.Id);
            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnPagedRequests()
        {
            // Arrange
            var service = CreateService();

            var request = CreateVerificationRequest();

            var pagedRequest = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            _verificationRepositoryMock
                .Setup(r => r.GetAllAsync(null, pagedRequest))
                .ReturnsAsync(new PagedResult<VerificationRequest>
                {
                    Items = new List<VerificationRequest> { request },
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 10
                });

            // Act
            var result = await service.GetAllAsync(null, pagedRequest);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items[0].Id.Should().Be(request.Id);
            result.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldThrow_WhenUserDoesNotExist()
        {
            // Arrange
            var service = CreateService();

            var pagedRequest = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync("missing-user"))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            Func<Task> act = async () =>
                await service.GetByUserIdAsync("missing-user", null, pagedRequest);

            // Assert
            await act.Should()
                .ThrowAsync<KeyNotFoundException>()
                .WithMessage("User not found.");
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldReturnPagedRequests_WhenUserExists()
        {
            // Arrange
            var service = CreateService();

            var user = CreateUser();
            var request = CreateVerificationRequest(userId: user.Id);

            var pagedRequest = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);

            _verificationRepositoryMock
                .Setup(r => r.GetByUserIdAsync(user.Id, null, pagedRequest))
                .ReturnsAsync(new PagedResult<VerificationRequest>
                {
                    Items = new List<VerificationRequest> { request },
                    TotalCount = 1,
                    Page = 1,
                    PageSize = 10
                });

            // Act
            var result = await service.GetByUserIdAsync(user.Id, null, pagedRequest);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items[0].UserId.Should().Be(user.Id);
            result.TotalCount.Should().Be(1);
        }
    }
}