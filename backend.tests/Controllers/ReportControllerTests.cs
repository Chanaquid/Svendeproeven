using System.Security.Claims;
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
    public class ReportControllerTests
    {
        private readonly Mock<IReportService> _reportServiceMock = new();

        private const string UserId = "user-123";
        private const string AdminId = "admin-123";

        private ReportController CreateController(bool isAdmin = false)
        {
            var controller = new ReportController(_reportServiceMock.Object);

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
        public async Task CreateReport_ShouldPassCurrentUserIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController();

            var dto = new CreateReportDto();

            var report = new ReportDto
            {
                Id = 1
            };

            _reportServiceMock
                .Setup(s => s.CreateReportAsync(UserId, dto))
                .ReturnsAsync(report);

            // Act
            var response = await controller.CreateReport(dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<ReportDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(report);
            apiResponse.Message.Should().Be("Report submitted successfully.");

            _reportServiceMock.Verify(
                s => s.CreateReportAsync(UserId, dto),
                Times.Once
            );
        }

        [Fact]
        public async Task GetMyReports_ShouldPassCurrentUserIdFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController();

            var filter = new ReportFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<ReportDto>
            {
                Items = new List<ReportDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _reportServiceMock
                .Setup(s => s.GetMyReportsAsync(UserId, filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetMyReports(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<ReportDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _reportServiceMock.Verify(
                s => s.GetMyReportsAsync(UserId, filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task GetById_ShouldPassIsAdminFalse_WhenNormalUser()
        {
            // Arrange
            var controller = CreateController(isAdmin: false);

            var reportId = 1;

            var report = new ReportDto
            {
                Id = reportId
            };

            _reportServiceMock
                .Setup(s => s.GetByIdAsync(reportId, UserId, false))
                .ReturnsAsync(report);

            // Act
            var response = await controller.GetById(reportId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<ReportDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(report);

            _reportServiceMock.Verify(
                s => s.GetByIdAsync(reportId, UserId, false),
                Times.Once
            );
        }

        [Fact]
        public async Task GetById_ShouldPassIsAdminTrue_WhenAdmin()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var reportId = 1;

            var report = new ReportDto
            {
                Id = reportId
            };

            _reportServiceMock
                .Setup(s => s.GetByIdAsync(reportId, AdminId, true))
                .ReturnsAsync(report);

            // Act
            var response = await controller.GetById(reportId);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<ReportDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(report);

            _reportServiceMock.Verify(
                s => s.GetByIdAsync(reportId, AdminId, true),
                Times.Once
            );
        }

        [Fact]
        public async Task GetAll_ShouldPassFilterAndRequestToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var filter = new ReportFilter();

            var request = new PagedRequest
            {
                Page = 1,
                PageSize = 10
            };

            var pagedResult = new PagedResult<ReportDto>
            {
                Items = new List<ReportDto>(),
                Page = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _reportServiceMock
                .Setup(s => s.GetAllAsync(filter, request))
                .ReturnsAsync(pagedResult);

            // Act
            var response = await controller.GetAll(filter, request);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<PagedResult<ReportDto>>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(pagedResult);

            _reportServiceMock.Verify(
                s => s.GetAllAsync(filter, request),
                Times.Once
            );
        }

        [Fact]
        public async Task ResolveReport_ShouldPassReportIdAdminIdAndDtoToService()
        {
            // Arrange
            var controller = CreateController(isAdmin: true);

            var reportId = 1;
            var dto = new AdminResolveReportDto();

            var report = new ReportDto
            {
                Id = reportId
            };

            _reportServiceMock
                .Setup(s => s.ResolveReportAsync(reportId, AdminId, dto))
                .ReturnsAsync(report);

            // Act
            var response = await controller.ResolveReport(reportId, dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<ReportDto>>()
                .Subject;

            apiResponse.Data.Should().BeSameAs(report);
            apiResponse.Message.Should().Be("Report resolved successfully.");

            _reportServiceMock.Verify(
                s => s.ResolveReportAsync(reportId, AdminId, dto),
                Times.Once
            );
        }
    }
}