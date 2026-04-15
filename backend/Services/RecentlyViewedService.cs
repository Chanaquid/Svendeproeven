using backend.Dtos;
using backend.Interfaces;
using backend.Models;

namespace backend.Services
{
    public class UserRecentlyViewedService : IUserRecentlyViewedService
    {
        private readonly IUserRecentlyViewedRepository _recentlyViewedRepository;
        private readonly IItemRepository _itemRepository;

        private const int MaxStored = 15; //buffer size (queue)
        private const int MaxShown = 10;  //how many the user sees

        public UserRecentlyViewedService(
            IUserRecentlyViewedRepository recentlyViewedRepository,
            IItemRepository itemRepository)
        {
            _recentlyViewedRepository = recentlyViewedRepository;
            _itemRepository = itemRepository;
        }

        public async Task TrackViewAsync(string userId, int itemId)
        {
            var existing = await _recentlyViewedRepository.GetAsync(userId, itemId);

            if (existing != null)
            {
                //Already in list — bump to top by updating timestamp
                existing.ViewedAt = DateTime.UtcNow;
                _recentlyViewedRepository.Update(existing);
                await _recentlyViewedRepository.SaveChangesAsync();
                return;
            }

            //New item — check if buffer is full before adding
            var count = await _recentlyViewedRepository.GetCountByUserIdAsync(userId);
            if (count >= MaxStored)
                await _recentlyViewedRepository.DeleteOldestAsync(userId);

            await _recentlyViewedRepository.AddAsync(new UserRecentlyViewedItem
            {
                UserId = userId,
                ItemId = itemId,
                ViewedAt = DateTime.UtcNow
            });

            await _recentlyViewedRepository.SaveChangesAsync();
        }

        public async Task<List<UserRecentlyViewedItemDto>> GetRecentlyViewedAsync(string userId)
        {
            var items = await _recentlyViewedRepository.GetByUserIdAsync(userId, MaxShown);

            return items
                .Where(r => r.Item != null && !r.Item.IsDeleted)
                .Select(r => new UserRecentlyViewedItemDto
                {
                    ItemId = r.ItemId,
                    ItemTitle = r.Item.Title,
                    ItemSlug = r.Item.Slug,
                    ItemMainPhotoUrl = r.Item.Photos.FirstOrDefault()?.PhotoUrl,
                    PricePerDay = r.Item.PricePerDay,
                    IsFree = r.Item.IsFree,
                    IsAvailable = r.Item.IsActive && r.Item.Availability == ItemAvailability.Available,
                    OwnerName = r.Item.Owner?.FullName ?? string.Empty,
                    ViewedAt = r.ViewedAt
                })
                .ToList();
        }
    }
}