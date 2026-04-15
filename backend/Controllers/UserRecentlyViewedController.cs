using backend.Dtos;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/recently-viewed")]
    [ApiController]
    [Authorize]
    public class UserRecentlyViewedController : BaseController
    {
        private readonly IUserRecentlyViewedService _recentlyViewedService;

        public UserRecentlyViewedController(IUserRecentlyViewedService recentlyViewedService)
        {
            _recentlyViewedService = recentlyViewedService;
        }

        // GET /api/recently-viewed
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<UserRecentlyViewedItemDto>>>> GetRecentlyViewed()
        {
            var result = await _recentlyViewedService.GetRecentlyViewedAsync(Caller.UserId);
            return Ok(ApiResponse<List<UserRecentlyViewedItemDto>>.Ok(result));
        }

        // POST /api/recently-viewed/{itemId}
        // Called by the frontend when a user opens an item page
        [HttpPost("{itemId}")]
        public async Task<ActionResult<ApiResponse<string>>> TrackView(int itemId)
        {
            await _recentlyViewedService.TrackViewAsync(Caller.UserId, itemId);
            return Ok(ApiResponse<string>.Ok(null));
        }
    }
}