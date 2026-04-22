using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/upload")]
    public class CloudinaryController: BaseController
    {
        private readonly CloudinaryService _cloudinaryService;

        public CloudinaryController(CloudinaryService cloudinaryService)
        {
            _cloudinaryService = cloudinaryService;
        }

        [HttpPost("image")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null) return BadRequest("No file");

            var url = await _cloudinaryService.UploadImageAsync(file);

            return Ok(new { url });
        }

    }

}
