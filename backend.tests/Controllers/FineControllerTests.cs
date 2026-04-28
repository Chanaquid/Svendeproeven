using System.Security.Claims;
using backend.Controllers;
using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace backend.Tests.Controllers
{
    public class FineControllerTests
    {
        private readonly Mock<IFineService> _fineServiceMock = new();

        private const string UserId = "user-123";
        private const string AdminId = "admin-123";

        private FineController CreateController(bool isAdmin = false)
        {
            var controller = new FineController(_fineServiceMock.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, isAdmin ? AdminId : UserId)
            };

            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
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
        public async Task GetMyFines_ShouldPassCurrentUserIdFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController();

            var filter = new FineFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<FineListDto>
            {
                Items = new List<FineListDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _fineServiceMock
                .Setup(s => s.GetMyFinesAsync(UserId, filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetMyFines(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<FineListDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _fineServiceMock.Verify(
                s => s.GetMyFinesAsync(UserId, filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task GetMyFineById_ShouldPassFineIdAndCurrentUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var fineId = 1;

            var fine = new FineDto
            {
                Id = fineId
            };

            _fineServiceMock
                .Setup(s => s.GetFineByIdAsync(fineId, UserId))
                .ReturnsAsync(fine);

            // Act
            var response = await controller.GetMyFineById(fineId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<FineDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(fine);

            _fineServiceMock.Verify(
                s => s.GetFineByIdAsync(fineId, UserId),
                Times.Once
            );
        }

        [Fact]
        public async Task SubmitPaymentProof_ShouldPassCurrentUserIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController();

            var dto = new SubmitPaymentProofDto();

            var fine = new FineDto
            {
                Id = 1
            };

            _fineServiceMock
                .Setup(s => s.SubmitPaymentProofAsync(UserId, dto))
                .ReturnsAsync(fine);

            // Act
            var response = await controller.SubmitPaymentProof(dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<FineDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(fine);
            apiResponse.Message.Should().Be("Payment proof submitted successfully.");

            _fineServiceMock.Verify(
                s => s.SubmitPaymentProofAsync(UserId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllFines_ShouldPassFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var filter = new FineFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<FineListDto>
            {
                Items = new List<FineListDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _fineServiceMock
                .Setup(s => s.GetAllFinesAsync(filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetAllFines(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<FineListDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _fineServiceMock.Verify(
                s => s.GetAllFinesAsync(filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task AdminGetFineById_ShouldCallAdminGetFineByIdAsync()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var fineId = 1;

            var fine = new FineDto
            {
                Id = fineId
            };

            _fineServiceMock
                .Setup(s => s.AdminGetFineByIdAsync(fineId))
                .ReturnsAsync(fine);

            // Act
            var response = await controller.GetFineById(fineId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<FineDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(fine);

            _fineServiceMock.Verify(
                s => s.AdminGetFineByIdAsync(fineId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetFinesByStatus_ShouldPassStatusFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var status = FineStatus.PendingVerification;

            var filter = new FineFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<FineListDto>
            {
                Items = new List<FineListDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _fineServiceMock
                .Setup(s => s.GetFinesByStatusAsync(status, filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetFinesByStatus(status, filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<FineListDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _fineServiceMock.Verify(
                s => s.GetFinesByStatusAsync(status, filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task GetFinesByLoan_ShouldPassLoanIdToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var loanId = 10;

            var fines = new List<FineDto>
            {
                new FineDto { Id = 1 }
            };

            _fineServiceMock
                .Setup(s => s.GetFinesByLoanIdAsync(loanId))
                .ReturnsAsync(fines);

            // Act
            var response = await controller.GetFinesByLoan(loanId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<List<FineDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(fines);

            _fineServiceMock.Verify(
                s => s.GetFinesByLoanIdAsync(loanId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetStats_ShouldReturnFineStats()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var stats = new FineStatsDto();

            _fineServiceMock
                .Setup(s => s.GetFineStatsAsync())
                .ReturnsAsync(stats);

            // Act
            var response = await controller.GetStats();

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<FineStatsDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(stats);

            _fineServiceMock.Verify(
                s => s.GetFineStatsAsync(),
                Times.Once
            );
        }

        [Fact]
        public async Task CreateLoanDisputeFine_ShouldPassAdminIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var dto = new CreateLoanDisputeFineDto();

            var fine = new FineDto
            {
                Id = 1
            };

            _fineServiceMock
                .Setup(s => s.CreateLoanDisputeFineAsync(AdminId, dto))
                .ReturnsAsync(fine);

            // Act
            var response = await controller.CreateLoanDisputeFine(dto);

            // Assert
            var createdResult = response.Result.Should().BeOfType<CreatedAtActionResult>().Subject;

            createdResult.ActionName.Should().Be(nameof(FineController.GetFineById));
            createdResult.RouteValues!["fineId"].Should().Be(fine.Id);

            var apiResponse = createdResult.Value
                .Should()
                .BeOfType<ApiResponse<FineDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(fine);
            apiResponse.Message.Should().Be("Fine issued successfully.");

            _fineServiceMock.Verify(
                s => s.CreateLoanDisputeFineAsync(AdminId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task CreateCustomFine_ShouldPassAdminIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var dto = new CreateCustomFineDto();

            var fine = new FineDto
            {
                Id = 1
            };

            _fineServiceMock
                .Setup(s => s.CreateCustomFineAsync(AdminId, dto))
                .ReturnsAsync(fine);

            // Act
            var response = await controller.CreateCustomFine(dto);

            // Assert
            var createdResult = response.Result.Should().BeOfType<CreatedAtActionResult>().Subject;

            createdResult.ActionName.Should().Be(nameof(FineController.GetFineById));
            createdResult.RouteValues!["fineId"].Should().Be(fine.Id);

            var apiResponse = createdResult.Value
                .Should()
                .BeOfType<ApiResponse<FineDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(fine);
            apiResponse.Message.Should().Be("Fine issued successfully.");

            _fineServiceMock.Verify(
                s => s.CreateCustomFineAsync(AdminId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task UpdateFine_ShouldReturnBadRequest_WhenRouteFineIdDoesNotMatchBodyFineId()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var routeFineId = 1;

            var dto = new UpdateFineDto
            {
                FineId = 2
            };

            // Act
            var response = await controller.UpdateFine(routeFineId, dto);

            // Assert
            var badRequestResult = response.Result.Should().BeOfType<BadRequestObjectResult>().Subject;

            var apiResponse = badRequestResult.Value
                .Should()
                .BeOfType<ApiResponse<FineDto>>()
                .Subject;

            apiResponse.Success.Should().BeFalse();
            apiResponse.Message.Should().Be("Route fineId does not match body FineId");

            _fineServiceMock.Verify(
                s => s.UpdateFineAsync(It.IsAny<string>(), It.IsAny<UpdateFineDto>()),
                Times.Never
            );
        }

        [Fact]
        public async Task UpdateFine_ShouldPassAdminIdAndDtoToService_WhenRouteFineIdMatchesBodyFineId()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var fineId = 1;

            var dto = new UpdateFineDto
            {
                FineId = fineId
            };

            var updatedFine = new FineDto
            {
                Id = fineId
            };

            _fineServiceMock
                .Setup(s => s.UpdateFineAsync(AdminId, dto))
                .ReturnsAsync(updatedFine);

            // Act
            var response = await controller.UpdateFine(fineId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<FineDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(updatedFine);
            apiResponse.Message.Should().Be("Fine updated successfully.");

            _fineServiceMock.Verify(
                s => s.UpdateFineAsync(AdminId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task VoidFine_ShouldPassAdminIdAndFineIdToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var fineId = 1;

            _fineServiceMock
                .Setup(s => s.VoidFineAsync(AdminId, fineId))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.VoidFine(fineId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Message.Should().Be("Fine voided successfully.");

            _fineServiceMock.Verify(
                s => s.VoidFineAsync(AdminId, fineId),
                Times.Once
            );
        }

        [Fact]
        public async Task VerifyPayment_ShouldReturnApprovedMessage_WhenPaymentIsApproved()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var fineId = 1;

            var dto = new AdminFineVerifyPaymentDto
            {
                IsApproved = true
            };

            var fine = new FineDto
            {
                Id = fineId
            };

            _fineServiceMock
                .Setup(s => s.VerifyPaymentAsync(AdminId, fineId, dto))
                .ReturnsAsync(fine);

            // Act
            var response = await controller.VerifyPayment(fineId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<FineDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(fine);
            apiResponse.Message.Should().Be("Payment approved successfully.");

            _fineServiceMock.Verify(
                s => s.VerifyPaymentAsync(AdminId, fineId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task VerifyPayment_ShouldReturnRejectedMessage_WhenPaymentIsRejected()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var fineId = 1;

            var dto = new AdminFineVerifyPaymentDto
            {
                IsApproved = false
            };

            var fine = new FineDto
            {
                Id = fineId
            };

            _fineServiceMock
                .Setup(s => s.VerifyPaymentAsync(AdminId, fineId, dto))
                .ReturnsAsync(fine);

            // Act
            var response = await controller.VerifyPayment(fineId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<FineDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(fine);
            apiResponse.Message.Should().Be("Payment proof rejected.");

            _fineServiceMock.Verify(
                s => s.VerifyPaymentAsync(AdminId, fineId, dto),
                Times.Once
            );
        }
    }
}