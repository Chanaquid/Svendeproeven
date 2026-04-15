using backend.Dtos;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/user-reviews")]
    [ApiController]
    [Authorize]
    public class UserReviewController : BaseController
    {
        private readonly IUserReviewService _reviewService;

        public UserReviewController(IUserReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        // POST /api/user-reviews
        [HttpPost]
        public async Task<ActionResult<ApiResponse<UserReviewDto>>> CreateReview(
            [FromBody] CreateUserReviewDto dto)
        {
            var result = await _reviewService.CreateReviewAsync(Caller.UserId, dto);
            return Ok(ApiResponse<UserReviewDto>.Ok(result, "Review submitted successfully."));
        }

        // PATCH /api/user-reviews/{id}
        [HttpPatch("{id}")]
        public async Task<ActionResult<ApiResponse<UserReviewDto>>> UpdateReview(
            int id,
            [FromBody] UpdateUserReviewDto dto)
        {
            var result = await _reviewService.UpdateReviewAsync(id, Caller.UserId, dto);
            return Ok(ApiResponse<UserReviewDto>.Ok(result, "Review updated successfully."));
        }

        // GET /api/user-reviews/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<UserReviewDto>>> GetById(int id)
        {
            var result = await _reviewService.GetByIdAsync(id, Caller.UserId, Caller.IsAdmin);
            return Ok(ApiResponse<UserReviewDto>.Ok(result));
        }

        // GET /api/user-reviews/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<ApiResponse<PagedResult<UserReviewListDto>>>> GetReviewsForUser(
            string userId,
            [FromQuery] UserReviewFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _reviewService.GetReviewsForUserAsync(userId, filter, request);
            return Ok(ApiResponse<PagedResult<UserReviewListDto>>.Ok(result));
        }

        // GET /api/user-reviews/user/{userId}/summary
        [HttpGet("user/{userId}/summary")]
        public async Task<ActionResult<ApiResponse<UserRatingSummaryDto>>> GetRatingSummary(string userId)
        {
            var result = await _reviewService.GetRatingSummaryAsync(userId);
            return Ok(ApiResponse<UserRatingSummaryDto>.Ok(result));
        }

        // GET /api/user-reviews/my/given
        [HttpGet("my/given")]
        public async Task<ActionResult<ApiResponse<PagedResult<UserReviewListDto>>>> GetMyGivenReviews(
            [FromQuery] UserReviewFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _reviewService.GetMyGivenReviewsAsync(Caller.UserId, filter, request);
            return Ok(ApiResponse<PagedResult<UserReviewListDto>>.Ok(result));
        }

        //Admin endpoints

        // GET /api/user-reviews
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedResult<UserReviewDto>>>> GetAll(
            [FromQuery] UserReviewFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _reviewService.GetAllAsync(filter, request);
            return Ok(ApiResponse<PagedResult<UserReviewDto>>.Ok(result));
        }

        // POST /api/user-reviews/admin
        [HttpPost("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<UserReviewDto>>> AdminCreateReview(
            [FromBody] AdminCreateUserReviewDto dto)
        {
            var result = await _reviewService.AdminCreateReviewAsync(Caller.UserId, dto);
            return Ok(ApiResponse<UserReviewDto>.Ok(result, "Admin review submitted successfully."));
        }

        // DELETE /api/user-reviews/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<string>>> AdminDeleteReview(int id)
        {
            await _reviewService.AdminDeleteReviewAsync(id);
            return Ok(ApiResponse<string>.Ok(null, "Review deleted successfully."));
        }
    }
}