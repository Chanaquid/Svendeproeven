using backend.Controllers;
using backend.Dtos;
using backend.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace backend.Tests.Controllers
{
    public class EmailControllerTests
    {
        private readonly Mock<IEmailService> _emailServiceMock = new();

        private EmailController CreateController()
        {
            return new EmailController(_emailServiceMock.Object);
        }

        [Fact]
        public async Task SendTestEmail_ShouldCallEmailServiceAndReturnOk()
        {
            // Arrange
            var controller = CreateController();

            var dto = new TestEmailDto
            {
                ToEmail = "test@example.com",
                Subject = "Test subject",
                Body = "Test body"
            };

            _emailServiceMock
                .Setup(s => s.SendEmailAsync(dto.ToEmail, dto.Subject, dto.Body))
                .Returns(Task.CompletedTask);

            // Act
            var response = await controller.SendTestEmail(dto);

            // Assert
            var okResult = response.Result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Message.Should().Be("Email sent successfully");

            _emailServiceMock.Verify(
                s => s.SendEmailAsync(dto.ToEmail, dto.Subject, dto.Body),
                Times.Once
            );
        }
    }
}