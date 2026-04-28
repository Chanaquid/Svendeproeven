using System.Security.Claims;
using backend.Common;
using backend.Controllers;
using backend.Dtos;
using backend.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace backend.Tests.Controllers
{
    public class AdminUserControllerTests
    {
        private readonly Mock<IAdminUserService> _adminUserServiceMock = new();
        private readonly Mock<IItemService> _itemServiceMock = new();
        private readonly Mock<ILoanService> _loanServiceMock = new();
        private readonly Mock<IAppealService> _appealServiceMock = new();
        private readonly Mock<IDisputeService> _disputeServiceMock = new();
        private readonly Mock<IScoreHistoryService> _scoreHistoryServiceMock = new();
        private readonly Mock<IVerificationRequestService> _verificationRequestServiceMock = new();
        private readonly Mock<ISupportService> _supportServiceMock = new();
        private readonly Mock<IFineService> _fineServiceMock = new();
        private readonly Mock<IReportService> _reportServiceMock = new();

        private const string AdminUserId = "admin-123";

        private AdminUserController CreateController()
        {
            var controller = new AdminUserController(
                _adminUserServiceMock.Object,
                _itemServiceMock.Object,
                _appealServiceMock.Object,
                _loanServiceMock.Object,
                _disputeServiceMock.Object,
                _scoreHistoryServiceMock.Object,
                _verificationRequestServiceMock.Object,
                _supportServiceMock.Object,
                _fineServiceMock.Object,
                _reportServiceMock.Object
            );

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, AdminUserId),
                new Claim(ClaimTypes.Role, Roles.Admin)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user
                }
            };

            return controller;
        }

        [Fact]
        public async Task GetAllUsers_ShouldReturnOkWithPagedUsers()
        {
            // Arrange
            var controller = CreateController();

            var filter = new UserFilter();
            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<AdminUserDto>
            {
                Items = new List<AdminUserDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _adminUserServiceMock
                .Setup(s => s.GetUsersAsync(filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetAllUsers(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<AdminUserDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _adminUserServiceMock.Verify(
                s => s.GetUsersAsync(filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllIncludingDeleted_ShouldReturnOkWithPagedUsers()
        {
            // Arrange
            var controller = CreateController();

            var filter = new UserFilter();
            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<AdminUserDto>
            {
                Items = new List<AdminUserDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _adminUserServiceMock
                .Setup(s => s.GetAllUsersIncludingDeletedAsync(filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetAllIncludingDeleted(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<AdminUserDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _adminUserServiceMock.Verify(
                s => s.GetAllUsersIncludingDeletedAsync(filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllBannedUsers_ShouldPassTempBansOnlyToService()
        {
            // Arrange
            var controller = CreateController();

            var filter = new UserFilter();
            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var tempBansOnly = true;

            var pagedResult = new PagedResult<AdminUserDto>
            {
                Items = new List<AdminUserDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _adminUserServiceMock
                .Setup(s => s.GetAllBannedUsersAsync(filter, request, tempBansOnly))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetAllBannedUsers(filter, request, tempBansOnly);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<AdminUserDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _adminUserServiceMock.Verify(
                s => s.GetAllBannedUsersAsync(filter, request, tempBansOnly),
                Times.Once
            );
        }

        [Fact]
        public async Task GetUserById_ShouldReturnUserFromService()
        {
            // Arrange
            var controller = CreateController();

            var userId = "user-123";

            var userDto = new AdminUserDto
            {
                Id = userId
            };

            _adminUserServiceMock
                .Setup(s => s.GetUserByIdWIthDetailsAsync(userId))
                .ReturnsAsync(userDto);

            // Act
            var response = await controller.GetUserById(userId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<AdminUserDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(userDto);

            _adminUserServiceMock.Verify(
                s => s.GetUserByIdWIthDetailsAsync(userId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetUserItems_ShouldSetOwnerIdOnFilter()
        {
            // Arrange
            var controller = CreateController();

            var userId = "user-123";

            var filter = new ItemFilter();
            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<ItemListDto>
            {
                Items = new List<ItemListDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _itemServiceMock
                .Setup(s => s.AdminGetAllAsync(filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetUserItems(userId, filter, request);

            // Assert
            filter.OwnerId.Should().Be(userId);

            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<ItemListDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _itemServiceMock.Verify(
                s => s.AdminGetAllAsync(filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task GetUserLoans_ShouldCallLoanServiceWithAdminTrue()
        {
            // Arrange
            var controller = CreateController();

            var userId = "user-123";
            var filter = new LoanFilter();
            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<LoanListDto>
            {
                Items = new List<LoanListDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _loanServiceMock
                .Setup(s => s.GetAllLoansByUserIdAsync(userId, filter, request, true))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetUserLoans(userId, filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<LoanListDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _loanServiceMock.Verify(
                s => s.GetAllLoansByUserIdAsync(userId, filter, request, true),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateUser_ShouldPassTargetUserIdAndAdminUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var targetUserId = "user-123";

            var dto = new AdminEditUserDto();

            var updatedUser = new AdminUserDto
            {
                Id = targetUserId
            };

            _adminUserServiceMock
                .Setup(s => s.AdminEditUserAsync(targetUserId, AdminUserId, dto))
                .ReturnsAsync(updatedUser);

            // Act
            var response = await controller.UpdateUser(targetUserId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<AdminUserDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(updatedUser);
            apiResponse.Message.Should().Be("User updated successfully.");

            _adminUserServiceMock.Verify(
                s => s.AdminEditUserAsync(targetUserId, AdminUserId, dto),
                Times.Once
            );
        }

        
        [Fact]
        public async Task BanUser_ShouldPassTargetUserIdAndAdminUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var targetUserId = "user-123";

            var dto = new BanUserDto
            {
                Reason = "Violation"
            };

            _adminUserServiceMock
                .Setup(s => s.BanUserAsync(targetUserId, AdminUserId, dto))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.BanUser(targetUserId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Message.Should().Be("User banned successfully.");

            _adminUserServiceMock.Verify(
                s => s.BanUserAsync(targetUserId, AdminUserId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task UnbanUser_ShouldPassTargetUserIdAndAdminUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var targetUserId = "user-123";

            var dto = new UnbanUserDto
            {
                Reason = "Ban lifted"
            };

            _adminUserServiceMock
                .Setup(s => s.UnbanUserAsync(targetUserId, AdminUserId, dto))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.UnbanUser(targetUserId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Message.Should().Be("User unbanned successfully.");

            _adminUserServiceMock.Verify(
                s => s.UnbanUserAsync(targetUserId, AdminUserId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task DeleteUser_ShouldPassTargetUserIdAdminUserIdAndNoteToService()
        {
            // Arrange
            var controller = CreateController();

            var targetUserId = "user-123";
            var note = "Deleted by admin";

            var deleteResult = new AdminDeleteResultDto();

            _adminUserServiceMock
                .Setup(s => s.AdminSoftDeleteUserAsync(targetUserId, AdminUserId, note))
                .ReturnsAsync(deleteResult);

            // Act
            var response = await controller.DeleteUser(targetUserId, note);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<AdminDeleteResultDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(deleteResult);
            apiResponse.Message.Should().Be("User deleted successfully.");

            _adminUserServiceMock.Verify(
                s => s.AdminSoftDeleteUserAsync(targetUserId, AdminUserId, note),
                Times.Once
            );
        }
    }
}