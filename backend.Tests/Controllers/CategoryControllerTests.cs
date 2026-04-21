using backend.Controllers;
using backend.Dtos;
using backend.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace backend.Tests.Controllers;

// CategoryController is the simplest controller in this project: every action
// just delegates to ICategoryService and wraps the result in ApiResponse.Ok(...).
// That makes it the perfect place to learn the core pattern:
//
//   1. Arrange: create a Mock<ICategoryService> and set up Setup(...).ReturnsAsync(...)
//   2. Act:     call the action method on a new controller instance
//   3. Assert:  unwrap the ActionResult -> OkObjectResult -> ApiResponse<T> -> Data
//
// Once you see how this works, every other simple controller (ReportController,
// NotificationController, UserFavoriteController, ...) follows the exact same shape.
public class CategoryControllerTests
{
    private readonly Mock<ICategoryService> _service = new(MockBehavior.Strict);
    private readonly CategoryController _sut; // "system under test"

    public CategoryControllerTests()
    {
        _sut = new CategoryController(_service.Object);
    }

    // ---------- GET /api/categories ----------

    [Fact]
    public async Task GetAll_Anonymous_PassesIsAdminFalse_AndReturnsOk()
    {
        // Arrange
        var categories = new List<CategoryDto>
        {
            new() { Id = 1, Name = "Tools",  Slug = "tools"  },
            new() { Id = 2, Name = "Books",  Slug = "books"  }
        };
        // GetAllAsync(false) — anonymous caller, isAdmin defaults to false
        _service.Setup(s => s.GetAllAsync(false)).ReturnsAsync(categories);

        ControllerTestHelper.SetAnonymous(_sut);

        // Act
        var result = await _sut.GetAll();

        // Assert — dig through the ActionResult layers
        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value.Should().BeOfType<ApiResponse<List<CategoryDto>>>().Subject;

        body.Success.Should().BeTrue();
        body.Data.Should().BeEquivalentTo(categories);

        _service.VerifyAll(); // Strict mocks: verifies the call actually happened
    }

    [Fact]
    public async Task GetAll_AuthenticatedAdmin_PassesIsAdminTrue()
    {
        var categories = new List<CategoryDto> { new() { Id = 1, Name = "Tools" } };
        _service.Setup(s => s.GetAllAsync(true)).ReturnsAsync(categories);

        ControllerTestHelper.SetUser(_sut, userId: "admin-1", isAdmin: true);

        var result = await _sut.GetAll();

        result.Result.Should().BeOfType<OkObjectResult>();
        _service.Verify(s => s.GetAllAsync(true), Times.Once);
    }

    [Fact]
    public async Task GetAll_AuthenticatedNonAdmin_PassesIsAdminFalse()
    {
        _service.Setup(s => s.GetAllAsync(false)).ReturnsAsync(new List<CategoryDto>());

        ControllerTestHelper.SetUser(_sut, userId: "user-1", isAdmin: false);

        await _sut.GetAll();

        _service.Verify(s => s.GetAllAsync(false), Times.Once);
    }

    // ---------- GET /api/categories/{id} ----------

    [Fact]
    public async Task GetById_ReturnsCategoryInOkResponse()
    {
        var dto = new CategoryDto { Id = 42, Name = "Garden", Slug = "garden" };
        _service.Setup(s => s.GetByIdAsync(42, false)).ReturnsAsync(dto);

        ControllerTestHelper.SetAnonymous(_sut);

        var result = await _sut.GetById(42);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value.Should().BeOfType<ApiResponse<CategoryDto>>().Subject;
        body.Data.Should().BeEquivalentTo(dto);
    }

    // ---------- GET /api/categories/slug/{slug} ----------

    [Fact]
    public async Task GetBySlug_PassesSlugThrough_AndReturnsOk()
    {
        var dto = new CategoryDto { Id = 5, Name = "Power Tools", Slug = "power-tools" };
        _service.Setup(s => s.GetBySlugAsync("power-tools")).ReturnsAsync(dto);

        var result = await _sut.GetBySlug("power-tools");

        var body = (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<CategoryDto>>().Subject;
        body.Data!.Slug.Should().Be("power-tools");
    }

    // ---------- POST /api/categories (admin only) ----------

    [Fact]
    public async Task Create_ReturnsOk_WithCreatedMessage()
    {
        var input = new CreateCategoryDto { Name = "Kitchen", Icon = "🍳" };
        var created = new CategoryDto { Id = 10, Name = "Kitchen", Slug = "kitchen", Icon = "🍳" };

        _service.Setup(s => s.CreateAsync(input)).ReturnsAsync(created);

        ControllerTestHelper.SetUser(_sut, userId: "admin-1", isAdmin: true);

        var result = await _sut.Create(input);

        var body = (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<CategoryDto>>().Subject;
        body.Success.Should().BeTrue();
        body.Message.Should().Be("Category created successfully.");
        body.Data.Should().BeEquivalentTo(created);
    }

    // ---------- PUT /api/categories/{id} ----------

    [Fact]
    public async Task Update_PassesIdAndDto_AndReturnsUpdated()
    {
        var input = new UpdateCategoryDto { Name = "Garden Tools", IsActive = true };
        var updated = new CategoryDto { Id = 7, Name = "Garden Tools", IsActive = true };

        _service.Setup(s => s.UpdateAsync(7, input)).ReturnsAsync(updated);

        var result = await _sut.Update(7, input);

        var body = (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<CategoryDto>>().Subject;
        body.Message.Should().Be("Category updated successfully.");
        body.Data!.Name.Should().Be("Garden Tools");
    }

    // ---------- PATCH /api/categories/{id}/toggle ----------

    [Fact]
    public async Task Toggle_CallsServiceAndReturnsSuccessMessage()
    {
        _service.Setup(s => s.ToggleActiveAsync(3)).Returns(Task.CompletedTask);

        var result = await _sut.Toggle(3);

        var body = (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<string>>().Subject;
        body.Success.Should().BeTrue();
        body.Message.Should().Be("Category status toggled successfully.");
        body.Data.Should().BeNull(); // controller passes null explicitly

        _service.Verify(s => s.ToggleActiveAsync(3), Times.Once);
    }

    // ---------- DELETE /api/categories/{id} ----------

    [Fact]
    public async Task Delete_CallsServiceAndReturnsSuccessMessage()
    {
        _service.Setup(s => s.DeleteAsync(99)).Returns(Task.CompletedTask);

        var result = await _sut.Delete(99);

        var body = (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<string>>().Subject;
        body.Message.Should().Be("Category deleted successfully.");

        _service.Verify(s => s.DeleteAsync(99), Times.Once);
    }

    // ---------- Exception propagation ----------
    // Your ExceptionMiddleware converts thrown exceptions into HTTP responses,
    // but at the unit-test level the controller does NOT swallow them — it just
    // awaits the service. A test-worthy detail: verify the controller doesn't
    // accidentally catch anything.
    [Fact]
    public async Task GetById_WhenServiceThrows_ExceptionBubblesUp()
    {
        _service.Setup(s => s.GetByIdAsync(404, false))
                .ThrowsAsync(new KeyNotFoundException("Category not found"));

        ControllerTestHelper.SetAnonymous(_sut);

        var act = async () => await _sut.GetById(404);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Category not found");
    }
}