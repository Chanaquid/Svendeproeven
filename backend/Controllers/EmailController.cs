using backend.Dtos;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    //FOR DEV PURPOSES ONLY
    [ApiController]
    [Route("api/email")]
    [Authorize]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("test")]
        public async Task<ActionResult<ApiResponse<string>>> SendTestEmail([FromBody] TestEmailDto dto)
        {
            await _emailService.SendEmailAsync(
                dto.ToEmail,
                dto.Subject,
                dto.Body
            );

            return Ok(ApiResponse<string>.Ok(null, "Email sent successfully"));
        }
    }

    public class TestEmailDto
    {
        public string ToEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = "Test Email";
        public string Body { get; set; } = "This is a test email from backend.";
    }
}
