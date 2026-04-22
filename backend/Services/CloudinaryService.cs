using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

public class CloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration config)
    {
        var account = new Account(
            config["Cloudinary:CloudName"],
            config["Cloudinary:ApiKey"],
            config["Cloudinary:ApiSecret"]
        );

        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadImageAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = "rentit"
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        return result.SecureUrl.ToString();
    }
}