using backend.Dtos;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/items")]
    [Authorize]
    public class ItemController : BaseController
    {
        private readonly IItemService _itemService;

        public ItemController(IItemService itemService)
        {
            _itemService = itemService;
        }

        //Public

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<PagedResult<ItemListDto>>>> GetAllApproved(
            [FromQuery] ItemFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _itemService.GetAllApprovedAsync(filter, request, Caller.UserId);
            return Ok(ApiResponse<PagedResult<ItemListDto>>.Ok(result));
        }

        [HttpGet("available")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<PagedResult<ItemListDto>>>> GetAvailable(
            [FromQuery] ItemFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _itemService.GetAvailableItemsAsync(filter, request, Caller.UserId);
            return Ok(ApiResponse<PagedResult<ItemListDto>>.Ok(result));
        }

        [HttpGet("nearby")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<PagedResult<ItemListDto>>>> GetNearby(
            [FromQuery] double lat,
            [FromQuery] double lon,
            [FromQuery] double radiusKm = 10,
            [FromQuery] ItemFilter? filter = null,
            [FromQuery] PagedRequest? request = null)
        {
            request ??= new PagedRequest();
            var result = await _itemService.GetNearbyItemsAsync(lat, lon, radiusKm, filter, request, Caller.UserId);
            return Ok(ApiResponse<PagedResult<ItemListDto>>.Ok(result));
        }

        [HttpGet("category/{categoryId:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<PagedResult<ItemListDto>>>> GetByCategory(
            int categoryId,
            [FromQuery] ItemFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _itemService.GetByCategoryAsync(categoryId, filter, request, Caller.UserId);
            return Ok(ApiResponse<PagedResult<ItemListDto>>.Ok(result));
        }

        [HttpGet("by-owner/{ownerId}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<PagedResult<ItemListDto>>>> GetPublicByOwner(
            string ownerId,
            [FromQuery] ItemFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _itemService.GetPublicByOwnerAsync(ownerId, filter, request, Caller.UserId);
            return Ok(ApiResponse<PagedResult<ItemListDto>>.Ok(result));
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<ItemDto>>> GetById(int id)
        {
            var item = await _itemService.GetByIdAsync(id, Caller.UserId);
            return Ok(ApiResponse<ItemDto>.Ok(item));
        }

        [HttpGet("slug/{slug}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<ItemDto>>> GetBySlug(string slug)
        {
            var item = await _itemService.GetBySlugAsync(slug, Caller.UserId);
            return Ok(ApiResponse<ItemDto>.Ok(item));
        }

        //Owner endpoints

        [HttpGet("my")]
        public async Task<ActionResult<ApiResponse<PagedResult<ItemListDto>>>> GetMyItems(
            [FromQuery] ItemFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _itemService.GetMyItemsAsync(Caller.UserId, filter, request);
            return Ok(ApiResponse<PagedResult<ItemListDto>>.Ok(result));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<ItemDto>>> CreateItem([FromBody] CreateItemDto dto)
        {
            var item = await _itemService.CreateItemAsync(Caller.UserId, dto);
            return CreatedAtAction(nameof(GetById), new { id = item.Id },
                ApiResponse<ItemDto>.Ok(item, "Item created and submitted for review."));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<ItemDto>>> UpdateItem(int id, [FromBody] UpdateItemDto dto)
        {
            var item = await _itemService.UpdateItemAsync(Caller.UserId, id, dto);
            return Ok(ApiResponse<ItemDto>.Ok(item, "Item updated successfully."));
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteItem(int id)
        {
            await _itemService.DeleteItemAsync(Caller.UserId, id);
            return Ok(ApiResponse<string>.Ok(null, "Item deleted successfully."));
        }

        [HttpPatch("{id:int}/active")]
        public async Task<ActionResult<ApiResponse<ItemDto>>> ToggleActive(
            int id,
            [FromBody] ToggleActiveStatusDto dto)
        {
            var item = await _itemService.ToggleActiveStatusAsync(Caller.UserId, id, dto.IsActive);
            return Ok(ApiResponse<ItemDto>.Ok(item));
        }

        [HttpGet("{id:int}/qrcode")]
        public async Task<ActionResult<ApiResponse<ItemQrCodeDto>>> GetQrCode(int id)
        {
            var qr = await _itemService.GetQrCodeAsync(Caller.UserId, id);
            return Ok(ApiResponse<ItemQrCodeDto>.Ok(qr));
        }

        //Photos

        [HttpPost("{id:int}/photos")]
        public async Task<ActionResult<ApiResponse<ItemDto>>> AddPhoto(
            int id,
            [FromBody] AddItemPhotoDto dto)
        {
            var item = await _itemService.AddPhotoAsync(Caller.UserId, id, dto);
            return Ok(ApiResponse<ItemDto>.Ok(item, "Photo added successfully."));
        }

        [HttpDelete("{id:int}/photos/{photoId:int}")]
        public async Task<ActionResult<ApiResponse<ItemDto>>> DeletePhoto(int id, int photoId)
        {
            var item = await _itemService.DeletePhotoAsync(Caller.UserId, id, photoId);
            return Ok(ApiResponse<ItemDto>.Ok(item, "Photo deleted successfully."));
        }

        [HttpPatch("{id:int}/photos/{photoId:int}/primary")]
        public async Task<ActionResult<ApiResponse<ItemDto>>> SetPrimaryPhoto(int id, int photoId)
        {
            var item = await _itemService.SetPrimaryPhotoAsync(Caller.UserId, id, photoId);
            return Ok(ApiResponse<ItemDto>.Ok(item, "Primary photo updated."));
        }

        //Admin endpoints

        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedResult<ItemListDto>>>> AdminGetAll(
            [FromQuery] ItemFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _itemService.AdminGetAllAsync(filter, request);
            return Ok(ApiResponse<PagedResult<ItemListDto>>.Ok(result));
        }

        [HttpGet("admin/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<ItemDto>>> AdminGetById(int id)
        {
            var item = await _itemService.AdminGetByIdAsync(id);
            return Ok(ApiResponse<ItemDto>.Ok(item));
        }

        [HttpGet("admin/pending")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedResult<ItemListDto>>>> GetPendingApprovals(
            [FromQuery] ItemFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _itemService.GetPendingApprovalsAsync(filter, request);
            return Ok(ApiResponse<PagedResult<ItemListDto>>.Ok(result));
        }

        [HttpGet("admin/pending/count")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<int>>> GetPendingCount()
        {
            var count = await _itemService.GetPendingApprovalsCountAsync();
            return Ok(ApiResponse<int>.Ok(count));
        }

        [HttpPost("admin/{id:int}/decide")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<ItemDto>>> DecideItem(
            int id,
            [FromBody] AdminDecideItemDto dto)
        {
            var item = await _itemService.DecideItemAsync(Caller.UserId, id, dto);
            var message = dto.IsApproved ? "Item approved successfully." : "Item rejected.";
            return Ok(ApiResponse<ItemDto>.Ok(item, message));
        }

        [HttpPatch("admin/{id:int}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<ItemDto>>> AdminUpdateStatus(
            int id,
            [FromBody] AdminUpdateItemStatusDto dto)
        {
            var item = await _itemService.AdminUpdateStatusAsync(Caller.UserId, id, dto);
            return Ok(ApiResponse<ItemDto>.Ok(item, "Item status updated."));
        }
    }
}