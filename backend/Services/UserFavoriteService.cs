using backend.Dtos;
using backend.Interfaces;
using backend.Models;

namespace backend.Services
{
    //Notify users fav needs work
    public class UserFavoriteService : IUserFavoriteService
    {
        private readonly IUserFavoriteRepository _userFavoriteRepository;
        private readonly IItemRepository _itemRepository;

        public UserFavoriteService(
            IUserFavoriteRepository userFavoriteRepository,
            IItemRepository itemRepository)
        {
            _userFavoriteRepository = userFavoriteRepository;
            _itemRepository = itemRepository;
        }

        public async Task<PagedResult<UserFavoriteItemListDto>> GetFavoritesAsync(string userId, PagedRequest request)
        {
            var result = await _userFavoriteRepository.GetAllByUserIdAsync(userId, request);
            return new PagedResult<UserFavoriteItemListDto>
            {
                Items = result.Items.Select(f => MapToFavoriteListDto(f)).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };
        }

        //Returns true if favorited, false if unfavorited
        public async Task<bool> ToggleFavoriteAsync(string userId, int itemId, bool notify)
        {
            var item = await _itemRepository.GetByIdAsync(itemId)
                ?? throw new KeyNotFoundException($"Item {itemId} not found");

            if (item.Status != ItemStatus.Approved || !item.IsActive)
                throw new InvalidOperationException("You can only favorite active, approved items.");

            if (item.OwnerId == userId)
                throw new InvalidOperationException("You cannot favorite your own item.");

            var existing = await _userFavoriteRepository.GetAsync(userId, itemId);

            if (existing != null)
            {
                _userFavoriteRepository.Remove(existing);
                await _userFavoriteRepository.SaveChangesAsync();
                return false;
            }

            await _userFavoriteRepository.AddAsync(new UserFavoriteItem
            {
                UserId = userId,
                ItemId = itemId,
                NotifyWhenAvailable = notify,
                SavedAt = DateTime.UtcNow
            });

            await _userFavoriteRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsFavoritedAsync(string userId, int itemId)
        {
            return await _userFavoriteRepository.ExistsAsync(userId, itemId);
        }

        public async Task UpdateNotifyPreferenceAsync(string userId, int itemId, bool notify)
        {
            var favorite = await _userFavoriteRepository.GetAsync(userId, itemId)
                ?? throw new KeyNotFoundException("Favorite not found.");

            favorite.NotifyWhenAvailable = notify;
            await _userFavoriteRepository.SaveChangesAsync();
        }

        private static UserFavoriteItemListDto MapToFavoriteListDto(UserFavoriteItem favorite)
        {
            var item = favorite.Item;
            var primary = item.Photos.FirstOrDefault(p => p.IsPrimary) ?? item.Photos.FirstOrDefault();

            return new UserFavoriteItemListDto
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                Slug = item.Slug,
                MainPhotoUrl = primary?.PhotoUrl,
                PickupAddress = item.PickupAddress,
                CategoryId = item.CategoryId,
                CategoryName = item.Category?.Name ?? "",
                CategorySlug = item.Category?.Slug ?? "",
                CategoryIcon = item.Category?.Icon,
                OwnerId = item.OwnerId,
                OwnerName = item.Owner?.FullName ?? "",
                OwnerUsername = item.Owner?.UserName ?? "",
                OwnerAvatarUrl = item.Owner?.AvatarUrl ?? "",
                OwnerScore = item.Owner?.Score ?? 0,
                IsOwnerVerified = item.Owner?.IsVerified ?? false,
                IsFree = item.IsFree,
                PricePerDay = item.PricePerDay,
                Condition = item.Condition,
                Availability = item.Availability,
                IsActive = item.IsActive,
                RequiresVerification = item.RequiresVerification,
                AverageRating = item.Reviews?.Any() == true
                    ? Math.Round(item.Reviews.Average(r => r.Rating), 1)
                    : null,
                TotalReviews = item.Reviews?.Count ?? 0,
                MinLoanDays = item.MinLoanDays,
                MaxLoanDays = item.MaxLoanDays,
                AvailableFrom = item.AvailableFrom,
                AvailableUntil = item.AvailableUntil,
                CreatedAt = item.CreatedAt,
                NotifyWhenAvailable = favorite.NotifyWhenAvailable,
                SavedAt = favorite.SavedAt,
            };
        }

    }
}