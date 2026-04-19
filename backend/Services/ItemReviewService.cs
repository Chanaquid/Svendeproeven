using backend.Dtos;
using backend.Interfaces;
using backend.Models;
using backend.Repositories;
using Microsoft.AspNetCore.Identity;

namespace backend.Services
{
    public class ItemReviewService : IItemReviewService
    {
     

        private readonly IItemReviewRepository _itemReviewRepository;
        private readonly IItemRepository _itemRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILoanRepository _loanRepository;

        public ItemReviewService(IItemReviewRepository itemReviewRepository,
            IItemRepository itemRepository,
            UserManager<ApplicationUser> userManager,
            ILoanRepository loanRepository)
        {
            _itemReviewRepository = itemReviewRepository;
            _itemRepository = itemRepository;
            _userManager = userManager;
            _loanRepository = loanRepository;
        }

        public async Task<ItemReviewDto> CreateItemReviewAsync(
            string reviewerId,
            CreateItemReviewDto dto)
        {
            var user = await _userManager.FindByIdAsync(reviewerId);
            var isAdmin = user != null && await _userManager.IsInRoleAsync(user, "Admin");

            //Users must provide a LoanId to prove they actually used the item.
            if (!isAdmin && !dto.LoanId.HasValue)
                throw new ArgumentException("LoanId is required for user reviews.");

            if (isAdmin && dto.ItemId == 0)
                throw new ArgumentException("ItemId is required for admin reviews.");

            int itemId;
            int? loanId = null;

            if (!isAdmin)
            {
                var loan = await _loanRepository.GetByIdWithDetailsAsync(dto.LoanId!.Value);
                if (loan == null)
                    throw new KeyNotFoundException($"Loan {dto.LoanId} not found.");

                //Ensure the reviewer was actually the borrower.
                if (loan.BorrowerId != reviewerId)
                    throw new UnauthorizedAccessException("Only the borrower of this loan can review the item.");

                if (loan.Status != LoanStatus.Completed)
                    throw new InvalidOperationException("You can only review an item after the loan is completed.");

                var existing = await _itemReviewRepository.GetItemReviewByLoanIdAsync(dto.LoanId!.Value);
                if (existing != null)
                    throw new InvalidOperationException("You have already reviewed this item for this loan.");

                itemId = loan.ItemId;
                loanId = loan.Id;
            }
            else
            {
                itemId = dto.ItemId;
                loanId = null;
            }

            if (dto.Rating < 1 || dto.Rating > 5)
                throw new ArgumentException("Rating must be between 1 and 5.");

            var review = new ItemReview
            {
                ItemId = itemId,
                LoanId = loanId,
                ReviewerId = reviewerId,
                Rating = dto.Rating,
                Comment = dto.Comment?.Trim(),
                IsAdminReview = isAdmin,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _itemReviewRepository.AddItemReviewAsync(review);
            await _itemReviewRepository.SaveChangesAsync();

            await UpdateItemAverageRatingAsync(itemId);

            //Ensure navigation properties are populated before mapping to DTO.
            await _itemReviewRepository.LoadReviewerAsync(review);

            return MapToItemReviewDto(review, reviewerId);
        }

        public async Task<ItemReviewDto> EditItemReviewAsync(
            int reviewId,
            string currentUserId,
            UpdateItemReviewDto dto)
        {
            var review = await _itemReviewRepository.GetItemReviewByIdAsync(reviewId)
                ?? throw new KeyNotFoundException("Review not found.");

            if (review.IsDeleted)
                throw new InvalidOperationException("Review is deleted.");

            if (review.ReviewerId != currentUserId)
                throw new UnauthorizedAccessException("You can only edit your own reviews.");

            if (dto.Rating < 1 || dto.Rating > 5)
                throw new ArgumentException("Rating must be between 1 and 5.");

            review.Rating = dto.Rating;
            review.Comment = dto.Comment?.Trim();
            review.IsEdited = true;
            review.EditedAt = DateTime.UtcNow;

            _itemReviewRepository.Update(review);
            await _itemReviewRepository.SaveChangesAsync();

            await UpdateItemAverageRatingAsync(review.ItemId);

            return MapToItemReviewDto(review, currentUserId);
        }

        public async Task DeleteItemReviewAsync(int reviewId, string userId)
        {
            var review = await _itemReviewRepository.GetItemReviewByIdAsync(reviewId)
                ?? throw new KeyNotFoundException("Review not found.");

            var admin = await _userManager.FindByIdAsync(userId);
            var isAdmin = admin != null && await _userManager.IsInRoleAsync(admin, "Admin");


            if (!isAdmin)
                throw new UnauthorizedAccessException("Only admins can delete reviews.");

            review.IsDeleted = true;
            review.DeletedByAdminId = userId;
            review.DeletedAt = DateTime.UtcNow;

            _itemReviewRepository.Update(review);
            await _itemReviewRepository.SaveChangesAsync();
            await UpdateItemAverageRatingAsync(review.ItemId);

        }

        public async Task<PagedResult<ItemReviewDto>> GetByItemIdAsync(
            int itemId,
            string? currentUserId,
            ItemReviewFilter? filter,
            PagedRequest request)
        {
            var result = await _itemReviewRepository.GetItemReviewsByItemIdAsync(itemId, filter, request);

            return new PagedResult<ItemReviewDto>
            {
                Items = result.Items
                    .Select(r => MapToItemReviewDto(r, currentUserId))
                    .ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        //Recalculates and persists AverageRating on the Item whenever reviews change

        private async Task UpdateItemAverageRatingAsync(int itemId)
        {
            var item = await _itemRepository.GetByIdAsync(itemId);
            if (item == null) return;

            var ratings = await _itemReviewRepository.GetRatingsByItemIdAsync(itemId);

            item.AverageRating = ratings.Count > 0
                ? Math.Round(ratings.Average(), 2)
                : null;

            _itemRepository.Update(item);
            await _itemRepository.SaveChangesAsync();
        }


        private static ItemReviewDto MapToItemReviewDto(ItemReview r, string? currentUserId = null)
        {
            return new ItemReviewDto
            {
                Id = r.Id,
                LoanId = r.LoanId,
                ItemId = r.ItemId,
                Rating = r.Rating,
                Comment = r.Comment,
                ReviewerId = r.ReviewerId,
                ReviewerName = r.Reviewer?.FullName ?? string.Empty,
                ReviewerUserName = r.Reviewer?.UserName ?? string.Empty,
                ReviewerAvatarUrl = r.Reviewer?.AvatarUrl,
                CreatedAt = r.CreatedAt,
                IsEdited = r.IsEdited,
                EditedAt = r.EditedAt,
                IsAdminReview = r.IsAdminReview,
                //Client-side helper to determine if the current user can edit this review.
                IsMine = currentUserId != null && r.ReviewerId == currentUserId
            };
        }
    
    }
}