using backend.Data;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class UserRecentlyViewedRepository : IUserRecentlyViewedRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRecentlyViewedRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserRecentlyViewedItem>> GetByUserIdAsync(string userId, int limit)
        {
            return await _context.UserRecentlyViewedItems
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .Include(r => r.Item)
                    //Efficiently load only the primary photo to reduce data transfer
                    .ThenInclude(i => i.Photos.Where(p => p.IsPrimary))
                .OrderByDescending(r => r.ViewedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<UserRecentlyViewedItem?> GetAsync(string userId, int itemId)
        {
            return await _context.UserRecentlyViewedItems
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ItemId == itemId);
        }

        public async Task AddAsync(UserRecentlyViewedItem item)
        {
            await _context.UserRecentlyViewedItems.AddAsync(item);
        }

        public async Task<int> GetCountByUserIdAsync(string userId)
        {
            return await _context.UserRecentlyViewedItems
                .CountAsync(r => r.UserId == userId);
        }

        public async Task DeleteOldestAsync(string userId)
        {
            //Identify the single least recently viewed record for this user
            var oldest = await _context.UserRecentlyViewedItems
                .Where(r => r.UserId == userId)
                .OrderBy(r => r.ViewedAt)
                .FirstOrDefaultAsync();

            if (oldest != null)
                _context.UserRecentlyViewedItems.Remove(oldest);
        }

        public void Update(UserRecentlyViewedItem item)
        {
            _context.UserRecentlyViewedItems.Update(item);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}