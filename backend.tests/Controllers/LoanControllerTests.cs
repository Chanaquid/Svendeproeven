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
    public class LoanControllerTests
    {
        private readonly Mock<ILoanService> _loanServiceMock = new();

        private const string UserId = "user-123";
        private const string AdminId = "admin-123";

        private LoanController CreateController(bool isAdmin = false)
        {
            var controller = new LoanController(_loanServiceMock.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, isAdmin ? AdminId : UserId)
            };

            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, Roles.Admin));
            }

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
        public async Task CreateLoan_ShouldPassCurrentUserIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController();

            var dto = new CreateLoanDto();

            var loan = new LoanDto
            {
                Id = 1
            };

            _loanServiceMock
                .Setup(s => s.CreateLoanAsync(UserId, dto))
                .ReturnsAsync(loan);

            // Act
            var response = await controller.CreateLoan(dto);

            // Assert
            var createdResult = response.Result.Should().BeOfType<CreatedAtActionResult>().Subject;

            createdResult.ActionName.Should().Be(nameof(LoanController.GetById));
            createdResult.RouteValues!["id"].Should().Be(loan.Id);

            var apiResponse = createdResult.Value
                .Should()
                .BeOfType<ApiResponse<LoanDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(loan);
            apiResponse.Message.Should().Be("Loan request submitted successfully.");

            _loanServiceMock.Verify(
                s => s.CreateLoanAsync(UserId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task CancelLoan_ShouldPassCurrentUserIdLoanIdAndReasonToService()
        {
            // Arrange
            var controller = CreateController();

            var loanId = 1;

            var dto = new CancelLoanDto
            {
                Reason = "No longer needed"
            };

            var loan = new LoanDto
            {
                Id = loanId
            };

            _loanServiceMock
                .Setup(s => s.CancelLoanAsync(UserId, loanId, dto.Reason))
                .ReturnsAsync(loan);

            // Act
            var response = await controller.CancelLoan(loanId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<LoanDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(loan);
            apiResponse.Message.Should().Be("Loan cancelled successfully.");

            _loanServiceMock.Verify(
                s => s.CancelLoanAsync(UserId, loanId, dto.Reason),
                Times.Once
            );
        }

        [Fact]
        public async Task RequestExtension_ShouldPassCurrentUserIdLoanIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController();

            var loanId = 1;
            var dto = new RequestExtensionDto();

            var loan = new LoanDto
            {
                Id = loanId
            };

            _loanServiceMock
                .Setup(s => s.RequestExtensionAsync(UserId, loanId, dto))
                .ReturnsAsync(loan);

            // Act
            var response = await controller.RequestExtension(loanId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<LoanDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(loan);
            apiResponse.Message.Should().Be("Extension request submitted.");

            _loanServiceMock.Verify(
                s => s.RequestExtensionAsync(UserId, loanId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task ConfirmPickup_ShouldPassCurrentUserIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController();

            var dto = new ScanQrCodeDto();

            var loan = new LoanDto
            {
                Id = 1
            };

            _loanServiceMock
                .Setup(s => s.ConfirmPickupAsync(UserId, dto))
                .ReturnsAsync(loan);

            // Act
            var response = await controller.ConfirmPickup(dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<LoanDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(loan);
            apiResponse.Message.Should().Be("Pickup confirmed. Loan is now active.");

            _loanServiceMock.Verify(
                s => s.ConfirmPickupAsync(UserId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task ConfirmReturn_ShouldPassCurrentUserIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController();

            var dto = new ScanQrCodeDto();

            var loan = new LoanDto
            {
                Id = 1
            };

            _loanServiceMock
                .Setup(s => s.ConfirmReturnAsync(UserId, dto))
                .ReturnsAsync(loan);

            // Act
            var response = await controller.ConfirmReturn(dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<LoanDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(loan);
            apiResponse.Message.Should().Be("Return confirmed. Loan completed.");

            _loanServiceMock.Verify(
                s => s.ConfirmReturnAsync(UserId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task DecideLoan_ShouldReturnApprovedMessage_WhenApproved()
        {
            // Arrange
            var controller = CreateController();

            var loanId = 1;

            var dto = new OwnerDecideLoanDto
            {
                IsApproved = true
            };

            var loan = new LoanDto
            {
                Id = loanId
            };

            _loanServiceMock
                .Setup(s => s.DecideLoanAsync(UserId, loanId, dto))
                .ReturnsAsync(loan);

            // Act
            var response = await controller.DecideLoan(loanId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<LoanDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(loan);
            apiResponse.Message.Should().Be("Loan approved.");

            _loanServiceMock.Verify(
                s => s.DecideLoanAsync(UserId, loanId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task DecideLoan_ShouldReturnRejectedMessage_WhenRejected()
        {
            // Arrange
            var controller = CreateController();

            var loanId = 1;

            var dto = new OwnerDecideLoanDto
            {
                IsApproved = false
            };

            var loan = new LoanDto
            {
                Id = loanId
            };

            _loanServiceMock
                .Setup(s => s.DecideLoanAsync(UserId, loanId, dto))
                .ReturnsAsync(loan);

            // Act
            var response = await controller.DecideLoan(loanId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<LoanDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(loan);
            apiResponse.Message.Should().Be("Loan rejected.");

            _loanServiceMock.Verify(
                s => s.DecideLoanAsync(UserId, loanId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task DecideExtension_ShouldReturnApprovedMessage_WhenApproved()
        {
            // Arrange
            var controller = CreateController();

            var loanId = 1;

            var dto = new DecideExtensionDto
            {
                IsApproved = true
            };

            var loan = new LoanDto
            {
                Id = loanId
            };

            _loanServiceMock
                .Setup(s => s.DecideExtensionAsync(UserId, loanId, dto))
                .ReturnsAsync(loan);

            // Act
            var response = await controller.DecideExtension(loanId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<LoanDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(loan);
            apiResponse.Message.Should().Be("Extension approved.");

            _loanServiceMock.Verify(
                s => s.DecideExtensionAsync(UserId, loanId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task DecideExtension_ShouldReturnRejectedMessage_WhenRejected()
        {
            // Arrange
            var controller = CreateController();

            var loanId = 1;

            var dto = new DecideExtensionDto
            {
                IsApproved = false
            };

            var loan = new LoanDto
            {
                Id = loanId
            };

            _loanServiceMock
                .Setup(s => s.DecideExtensionAsync(UserId, loanId, dto))
                .ReturnsAsync(loan);

            // Act
            var response = await controller.DecideExtension(loanId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<LoanDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(loan);
            apiResponse.Message.Should().Be("Extension rejected.");

            _loanServiceMock.Verify(
                s => s.DecideExtensionAsync(UserId, loanId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task GetById_ShouldPassLoanIdAndCurrentUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var loanId = 1;

            var loan = new LoanDto
            {
                Id = loanId
            };

            _loanServiceMock
                .Setup(s => s.GetByIdAsync(loanId, UserId))
                .ReturnsAsync(loan);

            // Act
            var response = await controller.GetById(loanId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<LoanDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(loan);

            _loanServiceMock.Verify(
                s => s.GetByIdAsync(loanId, UserId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetMyLoansAsBorrower_ShouldPassCurrentUserIdFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController();

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
                .Setup(s => s.GetMyLoansAsBorrowerAsync(UserId, filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetMyLoansAsBorrower(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<LoanListDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _loanServiceMock.Verify(
                s => s.GetMyLoansAsBorrowerAsync(UserId, filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task GetMyLoansAsLender_ShouldPassCurrentUserIdFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController();

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
                .Setup(s => s.GetMyLoansAsLenderAsync(UserId, filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetMyLoansAsLender(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<LoanListDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _loanServiceMock.Verify(
                s => s.GetMyLoansAsLenderAsync(UserId, filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task AdminGetAll_ShouldPassFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

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
                .Setup(s => s.AdminGetAllAsync(filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.AdminGetAll(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<LoanListDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _loanServiceMock.Verify(
                s => s.AdminGetAllAsync(filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task AdminGetById_ShouldPassLoanIdToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var loanId = 1;

            var loan = new LoanDto
            {
                Id = loanId
            };

            _loanServiceMock
                .Setup(s => s.AdminGetByIdAsync(loanId))
                .ReturnsAsync(loan);

            // Act
            var response = await controller.AdminGetById(loanId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<LoanDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(loan);

            _loanServiceMock.Verify(
                s => s.AdminGetByIdAsync(loanId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetPendingAdminApprovals_ShouldReturnPendingLoans()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var pendingLoans = new List<AdminPendingLoanDto>();

            _loanServiceMock
                .Setup(s => s.GetPendingAdminApprovalsAsync())
                .ReturnsAsync(pendingLoans);

            // Act
            var response = await controller.GetPendingAdminApprovals();

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<List<AdminPendingLoanDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pendingLoans);

            _loanServiceMock.Verify(
                s => s.GetPendingAdminApprovalsAsync(),
                Times.Once
            );
        }

        [Fact]
        public async Task GetPendingAdminApprovalsCount_ShouldReturnCount()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var count = 3;

            _loanServiceMock
                .Setup(s => s.GetPendingAdminApprovalsCountAsync())
                .ReturnsAsync(count);

            // Act
            var response = await controller.GetPendingAdminApprovalsCount();

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<int>>()
                .Subject;

            apiResponse.Data.Should().Be(count);

            _loanServiceMock.Verify(
                s => s.GetPendingAdminApprovalsCountAsync(),
                Times.Once
            );
        }

        [Fact]
        public async Task AdminReviewLoan_ShouldReturnForwardedMessage_WhenApproved()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var loanId = 1;

            var dto = new AdminReviewLoanDto
            {
                IsApproved = true
            };

            var loan = new LoanDto
            {
                Id = loanId
            };

            _loanServiceMock
                .Setup(s => s.AdminReviewLoanAsync(AdminId, loanId, dto))
                .ReturnsAsync(loan);

            // Act
            var response = await controller.AdminReviewLoan(loanId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<LoanDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(loan);
            apiResponse.Message.Should().Be("Loan forwarded to owner.");

            _loanServiceMock.Verify(
                s => s.AdminReviewLoanAsync(AdminId, loanId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task AdminReviewLoan_ShouldReturnRejectedMessage_WhenRejected()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var loanId = 1;

            var dto = new AdminReviewLoanDto
            {
                IsApproved = false
            };

            var loan = new LoanDto
            {
                Id = loanId
            };

            _loanServiceMock
                .Setup(s => s.AdminReviewLoanAsync(AdminId, loanId, dto))
                .ReturnsAsync(loan);

            // Act
            var response = await controller.AdminReviewLoan(loanId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<LoanDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(loan);
            apiResponse.Message.Should().Be("Loan rejected.");

            _loanServiceMock.Verify(
                s => s.AdminReviewLoanAsync(AdminId, loanId, dto),
                Times.Once
            );
        }
    }
}