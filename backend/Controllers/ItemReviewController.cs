using backend.Common;
using backend.Dtos;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/items/{itemId:int}/reviews")]
    [Authorize]
    public class ItemReviewController : BaseController
    {
        private readonly IItemReviewService _itemReviewService;

        public ItemReviewController(IItemReviewService itemReviewService)
        {
            _itemReviewService = itemReviewService;
        }

        // GET: api/items/{itemId}/reviews
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<PagedResult<ItemReviewDto>>>> GetByItem(
            int itemId,
            [FromQuery] ItemReviewFilter? filter,
            [FromQuery] PagedRequest request)
        {
            var result = await _itemReviewService.GetByItemIdAsync(itemId,Caller.UserId, filter, request);
            return Ok(ApiResponse<PagedResult<ItemReviewDto>>.Ok(result));
        }

        //POST: api/items/{itemId}/reviews
        //Borrower leaves a review after loan is completed
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ItemReviewDto>>> CreateReview(
            int itemId,
            [FromBody] CreateItemReviewDto dto)
        {
            dto.ItemId = itemId;

            var review = await _itemReviewService.CreateItemReviewAsync(Caller.UserId, dto);
            return CreatedAtAction(nameof(GetByItem), new { itemId },
                ApiResponse<ItemReviewDto>.Ok(review, "Review submitted successfully."));
        }

        //PUT: api/items/{itemId}/reviews/{reviewId}
        //Only the reviewer can edit their own review
        [HttpPut("{reviewId:int}")]
        public async Task<ActionResult<ApiResponse<ItemReviewDto>>> EditReview(
            int itemId,
            int reviewId,
            [FromBody] UpdateItemReviewDto dto)
        {
            var review = await _itemReviewService.EditItemReviewAsync(reviewId, Caller.UserId, dto);
            return Ok(ApiResponse<ItemReviewDto>.Ok(review, "Review updated successfully."));
        }

        // DELETE: api/items/{itemId}/reviews/{reviewId}
        // Admin only
        [HttpDelete("{reviewId:int}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ApiResponse<string>>> DeleteReview(
            int itemId,
            int reviewId)
        {
            await _itemReviewService.DeleteItemReviewAsync(reviewId, Caller.UserId);
            return Ok(ApiResponse<string>.Ok(null, "Review deleted successfully."));
        }

        // POST: api/items/{itemId}/reviews/admin
        [HttpPost("admin")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult<ApiResponse<ItemReviewDto>>> AdminCreateReview(
            int itemId,
            [FromBody] AdminCreateItemReviewDto dto)
        {
            var createDto = new CreateItemReviewDto
            {
                ItemId = itemId,
                LoanId = null,
                Rating = dto.Rating,
                Comment = dto.Comment
            };

            var review = await _itemReviewService.CreateItemReviewAsync(Caller.UserId, createDto);
            return CreatedAtAction(nameof(GetByItem), new { itemId },
                ApiResponse<ItemReviewDto>.Ok(review, "Admin review submitted successfully."));
        }
    }
}