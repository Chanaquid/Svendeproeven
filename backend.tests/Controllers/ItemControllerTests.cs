using backend.Controllers;
using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace backend.Tests.Controllers;

public class ItemControllerTests
{
    private readonly Mock<IItemService> _service = new(MockBehavior.Strict);
    private readonly ItemController _sut;

    private const string UserId = "user-1";
    private const string AdminId = "admin-1";

    public ItemControllerTests()
    {
        _sut = new ItemController(_service.Object);
        ControllerTestHelper.SetUser(_sut, userId: UserId);
    }

    private static PagedResult<ItemListDto> EmptyPaged() =>
        new() { Items = new(), TotalCount = 0, Page = 1, PageSize = 20 };

    private static PagedResult<ItemListDto> SinglePaged(int id = 1, string title = "Drill") =>
        new()
        {
            Items = new() { new ItemListDto { Id = id, Title = title } },
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

    [Fact]
    public async Task GetAllApproved_PassesFilterRequestAndCallerId()
    {
        var filter = new ItemFilter();
        var request = new PagedRequest { Page = 2, PageSize = 10 };
        var paged = SinglePaged();

        _service.Setup(s => s.GetAllApprovedAsync(filter, request, UserId))
                .ReturnsAsync(paged);

        var result = await _sut.GetAllApproved(filter, request);

        var body = (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<PagedResult<ItemListDto>>>().Subject;

        body.Data!.TotalCount.Should().Be(1);
        body.Data.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetNearby_WhenRequestIsNull_UsesDefaultPagedRequest()
    {
        PagedRequest? captured = null;

        _service
            .Setup(s => s.GetNearbyItemsAsync(
                55.0, 12.0, 10.0,
                It.IsAny<ItemFilter?>(),
                It.IsAny<PagedRequest>(),
                UserId))
            .Callback<double, double, double, ItemFilter?, PagedRequest, string?>(
                (_, _, _, _, req, _) => captured = req)
            .ReturnsAsync(EmptyPaged());

        await _sut.GetNearby(55.0, 12.0, 10.0, null, null);

        captured.Should().NotBeNull();
        captured!.Page.Should().Be(1);
        captured.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetNearby_UsesProvidedRadius()
    {
        var request = new PagedRequest { Page = 1, PageSize = 5 };

        _service.Setup(s => s.GetNearbyItemsAsync(
                40.0, -74.0, 25.0, null, request, UserId))
            .ReturnsAsync(EmptyPaged());

        var result = await _sut.GetNearby(40.0, -74.0, 25.0, null, request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByCategory_PassesCategoryId()
    {
        var filter = new ItemFilter();
        var request = new PagedRequest();

        _service.Setup(s => s.GetByCategoryAsync(7, filter, request, UserId))
                .ReturnsAsync(EmptyPaged());

        await _sut.GetByCategory(7, filter, request);

        _service.Verify(s => s.GetByCategoryAsync(7, filter, request, UserId), Times.Once);
    }

    [Fact]
    public async Task GetPublicByOwner_PassesOwnerIdAndCallerId()
    {
        var filter = new ItemFilter();
        var request = new PagedRequest();

        _service.Setup(s => s.GetPublicByOwnerAsync("owner-xyz", filter, request, UserId))
                .ReturnsAsync(SinglePaged());

        var result = await _sut.GetPublicByOwner("owner-xyz", filter, request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_ReturnsSingleItem()
    {
        var item = new ItemDto { Id = 42, Title = "Drill", OwnerId = "owner-1" };

        _service.Setup(s => s.GetByIdAsync(42, UserId)).ReturnsAsync(item);

        var result = await _sut.GetById(42);

        var body = (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<ItemDto>>().Subject;

        body.Data.Should().BeEquivalentTo(item);
    }

    [Fact]
    public async Task GetBySlug_PassesSlugAndCallerId()
    {
        var item = new ItemDto { Id = 1, Slug = "my-drill", Title = "Drill" };

        _service.Setup(s => s.GetBySlugAsync("my-drill", UserId)).ReturnsAsync(item);

        var result = await _sut.GetBySlug("my-drill");

        (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<ItemDto>>()
            .Which.Data!.Slug.Should().Be("my-drill");
    }

    [Fact]
    public async Task GetNewest_PassesCount_AndDoesNotRequireAuth()
    {
        _service.Setup(s => s.GetNewestListedAsync(4))
                .ReturnsAsync(new List<ItemListDto>());

        var result = await _sut.GetNewest(4);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetNewest_UsesDefaultCountWhenNotSpecified()
    {
        _service.Setup(s => s.GetNewestListedAsync(4))
                .ReturnsAsync(new List<ItemListDto>());

        await _sut.GetNewest();

        _service.Verify(s => s.GetNewestListedAsync(4), Times.Once);
    }

    [Fact]
    public async Task GetAvailableCount_ReturnsIntInsideApiResponse()
    {
        _service.Setup(s => s.GetAvailableCountAsync()).ReturnsAsync(123);

        var result = await _sut.GetAvailableCount();

        (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<int>>()
            .Which.Data.Should().Be(123);
    }

    [Fact]
    public async Task GetMyItems_UsesCallerUserId()
    {
        var filter = new ItemFilter();
        var request = new PagedRequest();

        _service.Setup(s => s.GetMyItemsAsync(UserId, filter, request))
                .ReturnsAsync(SinglePaged());

        await _sut.GetMyItems(filter, request);

        _service.Verify(s => s.GetMyItemsAsync(UserId, filter, request), Times.Once);
    }

    [Fact]
    public async Task CreateItem_ReturnsCreatedAtAction_WithCorrectRouteValues()
    {
        var input = new CreateItemDto
        {
            CategoryId = 1,
            Title = "Drill",
            Description = "A drill",
            CurrentValue = 500m,
            PricePerDay = 20m,
            Condition = ItemCondition.Good,
            PickupAddress = "Main St",
            PickupLatitude = 55.0,
            PickupLongitude = 12.0,
            AvailableFrom = DateTime.UtcNow,
            AvailableUntil = DateTime.UtcNow.AddDays(30)
        };

        var created = new ItemDto { Id = 77, Title = "Drill", OwnerId = UserId };

        _service.Setup(s => s.CreateItemAsync(UserId, input)).ReturnsAsync(created);

        var result = await _sut.CreateItem(input);

        var created201 = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created201.ActionName.Should().Be(nameof(ItemController.GetById));
        created201.RouteValues!["id"].Should().Be(77);

        var body = created201.Value.Should().BeOfType<ApiResponse<ItemDto>>().Subject;
        body.Data!.Id.Should().Be(77);
    }

    [Fact]
    public async Task UpdateItem_PassesCallerId_ItemId_AndDto()
    {
        var input = new UpdateItemDto { Title = "Renamed" };
        var updated = new ItemDto { Id = 5, Title = "Renamed" };

        _service.Setup(s => s.UpdateItemAsync(UserId, 5, input)).ReturnsAsync(updated);

        var result = await _sut.UpdateItem(5, input);

        (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<ItemDto>>()
            .Which.Message.Should().Be("Item updated successfully.");
    }

    [Fact]
    public async Task DeleteItem_CallsServiceWithCallerId()
    {
        _service.Setup(s => s.DeleteItemAsync(UserId, 9)).Returns(Task.CompletedTask);

        var result = await _sut.DeleteItem(9);

        (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<string>>()
            .Which.Message.Should().Be("Item deleted successfully.");
    }

    [Fact]
    public async Task ToggleActive_PassesIsActiveFlag()
    {
        var dto = new ToggleActiveStatusDto { IsActive = false };
        var updated = new ItemDto { Id = 3, IsActive = false };

        _service.Setup(s => s.ToggleActiveStatusAsync(UserId, 3, false))
                .ReturnsAsync(updated);

        var result = await _sut.ToggleActive(3, dto);

        (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<ItemDto>>()
            .Which.Data!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetQrCode_ReturnsQrData()
    {
        var qr = new ItemQrCodeDto { ItemId = 1, QrCode = "data:image/png;base64,AAA" };

        _service.Setup(s => s.GetQrCodeAsync(UserId, 1)).ReturnsAsync(qr);

        var result = await _sut.GetQrCode(1);

        (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<ItemQrCodeDto>>()
            .Which.Data!.QrCode.Should().StartWith("data:image/png");
    }

    [Fact]
    public async Task AddPhoto_PassesDtoToService()
    {
        var dto = new AddItemPhotoDto();
        var updated = new ItemDto { Id = 1 };

        _service.Setup(s => s.AddPhotoAsync(UserId, 1, dto)).ReturnsAsync(updated);

        var result = await _sut.AddPhoto(1, dto);

        (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<ItemDto>>()
            .Which.Message.Should().Be("Photo added successfully.");
    }

    [Fact]
    public async Task DeletePhoto_PassesBothIds()
    {
        var updated = new ItemDto { Id = 1 };

        _service.Setup(s => s.DeletePhotoAsync(UserId, 1, 50)).ReturnsAsync(updated);

        var result = await _sut.DeletePhoto(1, 50);

        (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<ItemDto>>()
            .Which.Message.Should().Be("Photo deleted successfully.");
    }

    [Fact]
    public async Task SetPrimaryPhoto_PassesBothIds()
    {
        var updated = new ItemDto { Id = 1 };

        _service.Setup(s => s.SetPrimaryPhotoAsync(UserId, 1, 50)).ReturnsAsync(updated);

        var result = await _sut.SetPrimaryPhoto(1, 50);

        (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<ItemDto>>()
            .Which.Message.Should().Be("Primary photo updated.");
    }

    [Fact]
    public async Task AdminGetAll_DelegatesToAdminGetAllAsync()
    {
        ControllerTestHelper.SetUser(_sut, userId: AdminId, isAdmin: true);

        var filter = new ItemFilter();
        var request = new PagedRequest();

        _service.Setup(s => s.AdminGetAllAsync(filter, request))
                .ReturnsAsync(EmptyPaged());

        var result = await _sut.AdminGetAll(filter, request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AdminGetById_ReturnsSingleItem()
    {
        ControllerTestHelper.SetUser(_sut, userId: AdminId, isAdmin: true);

        var item = new ItemDto { Id = 1 };

        _service.Setup(s => s.AdminGetByIdAsync(1)).ReturnsAsync(item);

        var result = await _sut.AdminGetById(1);

        (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<ItemDto>>()
            .Which.Data!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetPendingApprovals_DelegatesToService()
    {
        ControllerTestHelper.SetUser(_sut, userId: AdminId, isAdmin: true);

        var filter = new ItemFilter();
        var request = new PagedRequest();

        _service.Setup(s => s.GetPendingApprovalsAsync(filter, request))
                .ReturnsAsync(EmptyPaged());

        var result = await _sut.GetPendingApprovals(filter, request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPendingCount_ReturnsCount()
    {
        ControllerTestHelper.SetUser(_sut, userId: AdminId, isAdmin: true);

        _service.Setup(s => s.GetPendingApprovalsCountAsync()).ReturnsAsync(7);

        var result = await _sut.GetPendingCount();

        (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<int>>()
            .Which.Data.Should().Be(7);
    }

    [Theory]
    [InlineData(true, "Item approved successfully.")]
    [InlineData(false, "Item rejected.")]
    public async Task DecideItem_MessageDependsOnIsApproved(bool isApproved, string expectedMessage)
    {
        ControllerTestHelper.SetUser(_sut, userId: AdminId, isAdmin: true);

        var dto = new AdminDecideItemDto { IsApproved = isApproved };
        var updated = new ItemDto { Id = 1 };

        _service.Setup(s => s.DecideItemAsync(AdminId, 1, dto))
                .ReturnsAsync(updated);

        var result = await _sut.DecideItem(1, dto);

        (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<ItemDto>>()
            .Which.Message.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task AdminUpdateStatus_PassesAdminIdAndDto()
    {
        ControllerTestHelper.SetUser(_sut, userId: AdminId, isAdmin: true);

        var dto = new AdminUpdateItemStatusDto { Status = ItemStatus.Approved };
        var updated = new ItemDto { Id = 1, Status = ItemStatus.Approved };

        _service.Setup(s => s.AdminUpdateStatusAsync(AdminId, 1, dto))
                .ReturnsAsync(updated);

        var result = await _sut.AdminUpdateStatus(1, dto);

        (result.Result as OkObjectResult)!.Value
            .Should().BeOfType<ApiResponse<ItemDto>>()
            .Which.Message.Should().Be("Item status updated.");
    }
}