using System.Net;
using System.Text.Json;
using backend.Controllers;
using backend.Dtos;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace backend.Tests.Controllers
{
    public class UploadThingControllerTests
    {
        private class FakeHttpMessageHandler : HttpMessageHandler
        {
            private readonly Queue<HttpResponseMessage> _responses = new();

            public List<HttpRequestMessage> Requests { get; } = new();

            public void AddResponse(HttpResponseMessage response)
            {
                _responses.Enqueue(response);
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                Requests.Add(request);

                if (_responses.Count == 0)
                {
                    throw new InvalidOperationException("No fake HTTP response configured.");
                }

                return Task.FromResult(_responses.Dequeue());
            }
        }

        private static IFormFile CreateFormFile(
            string fileName = "avatar.png",
            string contentType = "image/png",
            byte[]? content = null)
        {
            content ??= new byte[] { 1, 2, 3, 4, 5 };

            var stream = new MemoryStream(content);

            return new FormFile(stream, 0, content.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        private static UploadThingController CreateController(
            HttpClient httpClient,
            string apiKey = "fake-api-key")
        {
            var factoryMock = new Mock<IHttpClientFactory>();

            factoryMock
                .Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["UploadThing:SecretKey"] = apiKey
                })
                .Build();

            return new UploadThingController(factoryMock.Object, config);
        }

        [Fact]
        public async Task UploadAvatar_ShouldReturnBadRequest_WhenFileIsNull()
        {
            // Arrange
            var handler = new FakeHttpMessageHandler();
            var httpClient = new HttpClient(handler);

            var controller = CreateController(httpClient);

            // Act
            var result = await controller.UploadAvatar(null!);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;

            badRequest.Value.Should().Be("No file provided");

            handler.Requests.Should().BeEmpty();
        }

        [Fact]
        public async Task UploadAvatar_ShouldReturnBadRequest_WhenFileIsEmpty()
        {
            // Arrange
            var handler = new FakeHttpMessageHandler();
            var httpClient = new HttpClient(handler);

            var controller = CreateController(httpClient);

            var emptyFile = CreateFormFile(content: Array.Empty<byte>());

            // Act
            var result = await controller.UploadAvatar(emptyFile);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;

            badRequest.Value.Should().Be("No file provided");

            handler.Requests.Should().BeEmpty();
        }

        [Fact]
        public async Task UploadAvatar_ShouldReturnBadRequest_WhenFileIsTooLarge()
        {
            // Arrange
            var handler = new FakeHttpMessageHandler();
            var httpClient = new HttpClient(handler);

            var controller = CreateController(httpClient);

            var largeContent = new byte[(4 * 1024 * 1024) + 1];
            var largeFile = CreateFormFile(content: largeContent);

            // Act
            var result = await controller.UploadAvatar(largeFile);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;

            badRequest.Value.Should().Be("File too large");

            handler.Requests.Should().BeEmpty();
        }

        [Fact]
        public async Task UploadAvatar_ShouldRequestPresignedUrlUploadFileAndReturnFileUrl()
        {
            // Arrange
            var handler = new FakeHttpMessageHandler();

            var uploadThingResponseJson = """
            {
                "data": [
                    {
                        "url": "https://fake-presigned-url.com/upload",
                        "fileUrl": "https://cdn.uploadthing.com/avatar.png"
                    }
                ]
            }
            """;

            handler.AddResponse(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(uploadThingResponseJson)
            });

            handler.AddResponse(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("")
            });

            var httpClient = new HttpClient(handler);

            var controller = CreateController(httpClient, apiKey: "test-secret-key");

            var file = CreateFormFile(
                fileName: "avatar.png",
                contentType: "image/png",
                content: new byte[] { 1, 2, 3, 4, 5 });

            // Act
            var result = await controller.UploadAvatar(file);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            var apiResponse = okResult.Value
                .Should()
                .BeOfType<ApiResponse<string>>()
                .Subject;

            apiResponse.Data.Should().Be("https://cdn.uploadthing.com/avatar.png");
            apiResponse.Message.Should().Be("Upload Successful.");

            handler.Requests.Should().HaveCount(2);

            var presignedUrlRequest = handler.Requests[0];

            presignedUrlRequest.Method.Should().Be(HttpMethod.Post);
            presignedUrlRequest.RequestUri!.ToString().Should().Be("https://api.uploadthing.com/v6/uploadFiles");
            presignedUrlRequest.Headers.GetValues("x-uploadthing-api-key")
                .Should()
                .ContainSingle("test-secret-key");

            var uploadFileRequest = handler.Requests[1];

            uploadFileRequest.Method.Should().Be(HttpMethod.Put);
            uploadFileRequest.RequestUri!.ToString().Should().Be("https://fake-presigned-url.com/upload");
            uploadFileRequest.Content!.Headers.ContentType!.MediaType.Should().Be("image/png");
        }
    }
}