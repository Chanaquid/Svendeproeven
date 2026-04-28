using System.Security.Claims;
using backend.Common;
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
    public class DisputeControllerTests
    {
        private readonly Mock<IDisputeService> _disputeServiceMock = new();

        private const string UserId = "user-123";
        private const string AdminId = "admin-123";

        private DisputeController CreateController(bool isAdmin = false)
        {
            var controller = new DisputeController(_disputeServiceMock.Object);

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
        public async Task CreateDispute_ShouldPassCurrentUserIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController();

            var dto = new CreateDisputeDto();

            var dispute = new DisputeDto
            {
                Id = 1
            };

            _disputeServiceMock
                .Setup(s => s.CreateDisputeAsync(UserId, dto))
                .ReturnsAsync(dispute);

            // Act
            var response = await controller.CreateDispute(dto);

            // Assert
            var createdResult = response.Result.Should().BeOfType<CreatedAtActionResult>().Subject;

            createdResult.ActionName.Should().Be(nameof(DisputeController.GetDisputeById));
            createdResult.RouteValues!["id"].Should().Be(dispute.Id);

            var apiResponse = createdResult.Value
                .Should()
                .BeOfType<ApiResponse<DisputeDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(dispute);
            apiResponse.Message.Should().Be("Dispute filed successfully.");

            _disputeServiceMock.Verify(
                s => s.CreateDisputeAsync(UserId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task EditDispute_ShouldPassCurrentUserIdDisputeIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController();

            var disputeId = 1;
            var dto = new EditDisputeDto();

            var updatedDispute = new DisputeDto
            {
                Id = disputeId
            };

            _disputeServiceMock
                .Setup(s => s.EditDisputeAsync(UserId, disputeId, dto))
                .ReturnsAsync(updatedDispute);

            // Act
            var response = await controller.EditDispute(disputeId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<DisputeDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(updatedDispute);
            apiResponse.Message.Should().Be("Dispute updated successfully.");

            _disputeServiceMock.Verify(
                s => s.EditDisputeAsync(UserId, disputeId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task CancelDispute_ShouldPassCurrentUserIdAndDisputeIdToService()
        {
            // Arrange
            var controller = CreateController();

            var disputeId = 1;

            _disputeServiceMock
                .Setup(s => s.CancelDisputeAsync(UserId, disputeId))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.CancelDispute(disputeId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Message.Should().Be("Dispute cancelled.");

            _disputeServiceMock.Verify(
                s => s.CancelDisputeAsync(UserId, disputeId),
                Times.Once
            );
        }

        [Fact]
        public async Task AddFiledByPhoto_ShouldPassCurrentUserIdDisputeIdAndPhotoUrlToService()
        {
            // Arrange
            var controller = CreateController();

            var disputeId = 1;

            var dto = new AddDisputePhotoDto
            {
                PhotoUrl = "https://example.com/photo.jpg",
                Caption = null
            };

            var photo = new DisputePhotoDto
            {
                Id = 10
            };

            _disputeServiceMock
                .Setup(s => s.AddFiledByPhotoUrlAsync(UserId, disputeId, dto.PhotoUrl, dto.Caption))
                .ReturnsAsync(photo);

            // Act
            var response = await controller.AddFiledByPhoto(disputeId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<DisputePhotoDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(photo);
            apiResponse.Message.Should().Be("Photo added successfully.");

            _disputeServiceMock.Verify(
                s => s.AddFiledByPhotoUrlAsync(UserId, disputeId, dto.PhotoUrl, dto.Caption),
                Times.Once
            );
        }


        [Fact]
        public async Task SubmitResponse_ShouldPassCurrentUserIdDisputeIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController();

            var disputeId = 1;
            var dto = new SubmitDisputeResponseDto();

            var dispute = new DisputeDto
            {
                Id = disputeId
            };

            _disputeServiceMock
                .Setup(s => s.SubmitResponseAsync(UserId, disputeId, dto))
                .ReturnsAsync(dispute);

            // Act
            var response = await controller.SubmitResponse(disputeId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<DisputeDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(dispute);
            apiResponse.Message.Should().Be("Response submitted successfully.");

            _disputeServiceMock.Verify(
                s => s.SubmitResponseAsync(UserId, disputeId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task GetDisputeById_ShouldPassDisputeIdAndCurrentUserIdToService()
        {
            // Arrange
            var controller = CreateController();

            var disputeId = 1;

            var dispute = new DisputeDto
            {
                Id = disputeId
            };

            _disputeServiceMock
                .Setup(s => s.GetDisputeByIdAsync(disputeId, UserId))
                .ReturnsAsync(dispute);

            // Act
            var response = await controller.GetDisputeById(disputeId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<DisputeDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(dispute);

            _disputeServiceMock.Verify(
                s => s.GetDisputeByIdAsync(disputeId, UserId),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAllMyDisputes_ShouldPassCurrentUserIdFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController();

            var filter = new DisputeFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<DisputeListDto>
            {
                Items = new List<DisputeListDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _disputeServiceMock
                .Setup(s => s.GetAllDisputesByUserIdAsync(UserId, filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetAllMyDisputes(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<DisputeListDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _disputeServiceMock.Verify(
                s => s.GetAllDisputesByUserIdAsync(UserId, filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task CanFileDispute_ShouldReturnCanFileObject()
        {
            // Arrange
            var controller = CreateController();

            var loanId = 50;

            _disputeServiceMock
                .Setup(s => s.CanUserFileDisputeAsync(UserId, loanId))
                .ReturnsAsync(true);

            // Act
            var response = await controller.CanFileDispute(loanId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<object>>()
                .Subject;

            apiResponse.Data.Should().NotBeNull();

            _disputeServiceMock.Verify(
                s => s.CanUserFileDisputeAsync(UserId, loanId),
                Times.Once
            );
        }

        [Fact]
        public async Task AdminGetDisputeById_ShouldCallAdminServiceMethod()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var disputeId = 1;

            var dispute = new DisputeDto
            {
                Id = disputeId
            };

            _disputeServiceMock
                .Setup(s => s.AdminGetDisputeByIdAsync(disputeId, AdminId))
                .ReturnsAsync(dispute);

            // Act
            var response = await controller.AdminGetDisputeById(disputeId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<DisputeDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(dispute);

            _disputeServiceMock.Verify(
                s => s.AdminGetDisputeByIdAsync(disputeId, AdminId),
                Times.Once
            );
        }




        [Fact]
        public async Task GetAllDisputes_ShouldPassFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var filter = new DisputeFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<DisputeListDto>
            {
                Items = new List<DisputeListDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _disputeServiceMock
                .Setup(s => s.GetAllDisputesAsync(AdminId, filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetAllDisputes(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<DisputeListDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _disputeServiceMock.Verify(
                s => s.GetAllDisputesAsync(AdminId, filter, request),
                Times.Once
            );
        }



        [Fact]
        public async Task ResolveDispute_ShouldPassAdminIdDisputeIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var disputeId = 1;
            var dto = new AdminResolveDisputeDto();

            var resolvedDispute = new DisputeDto
            {
                Id = disputeId
            };

            _disputeServiceMock
                .Setup(s => s.ResolveDisputeAsync(AdminId, disputeId, dto))
                .ReturnsAsync(resolvedDispute);

            // Act
            var response = await controller.ResolveDispute(disputeId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<DisputeDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(resolvedDispute);
            apiResponse.Message.Should().Be("Dispute resolved successfully.");

            _disputeServiceMock.Verify(
                s => s.ResolveDisputeAsync(AdminId, disputeId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task GetDisputeStats_ShouldReturnStats()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var stats = new DisputeStatsDto();

            _disputeServiceMock
                .Setup(s => s.GetDisputeStatsAsync())
                .ReturnsAsync(stats);

            // Act
            var response = await controller.GetDisputeStats();

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<DisputeStatsDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(stats);

            _disputeServiceMock.Verify(
                s => s.GetDisputeStatsAsync(),
                Times.Once
            );
        }
    }
}