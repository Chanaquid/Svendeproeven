using backend.Dtos;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/favorites")]
    [Authorize]
    public class UserFavoriteController : BaseController
    {
        private readonly IUserFavoriteService _favoriteService;

        public UserFavoriteController(IUserFavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<UserFavoriteItemListDto>>>> GetMyFavorites(
            [FromQuery] PagedRequest request)
        {
            var result = await _favoriteService.GetFavoritesAsync(Caller.UserId, request);
            return Ok(ApiResponse<PagedResult<UserFavoriteItemListDto>>.Ok(result, "Favorites retrieved successfully"));
        }

        // POST /api/favorites/{itemId}
        [HttpPost("{itemId:int}")]
        public async Task<ActionResult<ApiResponse<FavoriteToggleResultDto>>> Toggle(
            int itemId,
            [FromQuery] bool notify = false)
        {

            var isFavorited = await _favoriteService.ToggleFavoriteAsync(Caller.UserId, itemId, notify);
            var data = new FavoriteToggleResultDto { ItemId = itemId, IsFavorited = isFavorited };
            var message = isFavorited ? "Item added to favorites" : "Item removed from favorites";
            return Ok(ApiResponse<FavoriteToggleResultDto>.Ok(data, message));

        }

        // GET /api/favorites/{itemId}/status
        [HttpGet("{itemId:int}/status")]
        public async Task<ActionResult<ApiResponse<FavoriteStatusDto>>> GetStatus(int itemId)
        {
            var isFavorited = await _favoriteService.IsFavoritedAsync(Caller.UserId, itemId);
            var data = new FavoriteStatusDto { ItemId = itemId, IsFavorited = isFavorited };
            return Ok(ApiResponse<FavoriteStatusDto>.Ok(data));
        }

        // PATCH /api/favorites/{itemId}/notify
        [HttpPatch("{itemId:int}/notify")]
        public async Task<ActionResult<ApiResponse<NotifyPreferenceResultDto>>> UpdateNotifyPreference(
            int itemId,
            [FromBody] UpdateNotifyPreferenceDto dto)
        {

            await _favoriteService.UpdateNotifyPreferenceAsync(Caller.UserId, itemId, dto.Notify);
            var data = new NotifyPreferenceResultDto { ItemId = itemId, NotifyWhenAvailable = dto.Notify };
            return Ok(ApiResponse<NotifyPreferenceResultDto>.Ok(data, "Notification preference updated"));

        }
    }
}