using backend.Controllers;
using backend.Dtos;
using backend.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace backend.Tests.Controllers;

public class CategoryControllerTests
{
    private readonly Mock<ICategoryService> _service = new(MockBehavior.Strict);
    private readonly CategoryController _sut;

    public CategoryControllerTests()
    {
        _sut = new CategoryController(_service.Object);
    }

    [Fact]
    public async Task GetAll_Anonymous_ReturnsOk()
    {
        var categories = new List<CategoryDto>
        {
            new() { Id = 1, Name = "Tools", Slug = "tools" },
            new() { Id = 2, Name = "Books", Slug = "books" }
        };

        _service.Setup(s => s.GetAllAsync(false)).ReturnsAsync(categories);

        ControllerTestHelper.SetAnonymous(_sut);

        var result = await _sut.GetAll();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value.Should().BeOfType<ApiResponse<List<CategoryDto>>>().Subject;

        body.Data.Should().BeEquivalentTo(categories);
    }

    [Fact]
    public async Task GetAll_Admin_ReturnsAdminView()
    {
        _service.Setup(s => s.GetAllAsync(true)).ReturnsAsync(new List<CategoryDto>());

        ControllerTestHelper.SetUser(_sut, "admin-1", isAdmin: true);

        await _sut.GetAll();

        _service.Verify(s => s.GetAllAsync(true), Times.Once);
    }

    [Fact]
    public async Task GetAll_User_ReturnsNonAdminView()
    {
        _service.Setup(s => s.GetAllAsync(false)).ReturnsAsync(new List<CategoryDto>());

        ControllerTestHelper.SetUser(_sut, "user-1", isAdmin: false);

        await _sut.GetAll();

        _service.Verify(s => s.GetAllAsync(false), Times.Once);
    }

    [Fact]
    public async Task GetById_ReturnsOk()
    {
        var dto = new CategoryDto { Id = 42, Name = "Garden", Slug = "garden" };

        _service.Setup(s => s.GetByIdAsync(42, false)).ReturnsAsync(dto);

        ControllerTestHelper.SetAnonymous(_sut);

        var result = await _sut.GetById(42);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value.Should().BeOfType<ApiResponse<CategoryDto>>().Subject;

        body.Data.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task GetBySlug_ReturnsOk()
    {
        var dto = new CategoryDto { Id = 5, Name = "Power Tools", Slug = "power-tools" };

        _service.Setup(s => s.GetBySlugAsync("power-tools")).ReturnsAsync(dto);

        var result = await _sut.GetBySlug("power-tools");

        var body = (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<CategoryDto>>().Subject;

        body.Data!.Slug.Should().Be("power-tools");
    }

    [Fact]
    public async Task Create_ReturnsOk()
    {
        var input = new CreateCategoryDto { Name = "Kitchen", Icon = "🍳" };

        var created = new CategoryDto { Id = 10, Name = "Kitchen", Slug = "kitchen", Icon = "🍳" };

        _service.Setup(s => s.CreateAsync(input)).ReturnsAsync(created);

        ControllerTestHelper.SetUser(_sut, "admin-1", isAdmin: true);

        var result = await _sut.Create(input);

        var body = (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<CategoryDto>>().Subject;

        body.Data.Should().BeEquivalentTo(created);
    }

    [Fact]
    public async Task Update_ReturnsUpdated()
    {
        var input = new UpdateCategoryDto { Name = "Garden Tools", IsActive = true };

        var updated = new CategoryDto { Id = 7, Name = "Garden Tools", IsActive = true };

        _service.Setup(s => s.UpdateAsync(7, input)).ReturnsAsync(updated);

        var result = await _sut.Update(7, input);

        var body = (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<CategoryDto>>().Subject;

        body.Data!.Name.Should().Be("Garden Tools");
    }

    [Fact]
    public async Task Toggle_ReturnsOk()
    {
        _service.Setup(s => s.ToggleActiveAsync(3)).Returns(Task.CompletedTask);

        var result = await _sut.Toggle(3);

        var body = (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<string>>().Subject;

        body.Success.Should().BeTrue();

        _service.Verify(s => s.ToggleActiveAsync(3), Times.Once);
    }

    [Fact]
    public async Task Delete_ReturnsOk()
    {
        _service.Setup(s => s.DeleteAsync(99)).Returns(Task.CompletedTask);

        var result = await _sut.Delete(99);

        var body = (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<string>>().Subject;

        _service.Verify(s => s.DeleteAsync(99), Times.Once);
    }

    [Fact]
    public async Task GetById_ServiceThrows_ExceptionBubblesUp()
    {
        _service.Setup(s => s.GetByIdAsync(404, false))
            .ThrowsAsync(new KeyNotFoundException("Category not found"));

        ControllerTestHelper.SetAnonymous(_sut);

        var act = async () => await _sut.GetById(404);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}