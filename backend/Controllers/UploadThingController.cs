using backend.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadThingController : ControllerBase
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public UploadThingController(IHttpClientFactory factory, IConfiguration config)
        {
            _http = factory.CreateClient();
            _config = config;
        }

        [HttpPost("avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided");

            if (file.Length > 4 * 1024 * 1024)
                return BadRequest("File too large");

            var apiKey = _config["UploadThing:SecretKey"];

            //Get presigned URL from UploadThing
            var payload = new
            {
                files = new[] { new { name = file.FileName, size = file.Length, type = file.ContentType } }
            };

            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.uploadthing.com/v6/uploadFiles");
            req.Headers.Add("x-uploadthing-api-key", apiKey);
            req.Content = JsonContent.Create(payload);

            var res = await _http.SendAsync(req);
            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            var fileData = json.GetProperty("data")[0];

            var presignedUrl = fileData.GetProperty("url").GetString()!;
            var fileUrl = fileData.GetProperty("fileUrl").GetString()!;

            //Upload actual file bytes to presigned URL
            using var stream = file.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

            await _http.PutAsync(presignedUrl, fileContent);

            //Return the public URL to Angular
            return Ok(ApiResponse<string>.Ok(fileUrl, "Upload Successful."));
        }
    }
}
