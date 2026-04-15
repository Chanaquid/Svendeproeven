using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace backend.Repositories
{
    public class UserFavoriteRepository : IUserFavoriteRepository
    {
        private readonly ApplicationDbContext _context;

        public UserFavoriteRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<UserFavoriteItem>> GetAllByUserIdAsync(string userId, PagedRequest request)
        {
            var query = _context.UserFavoriteItems
                .AsNoTracking()
                .Include(f => f.Item)
                    .ThenInclude(i => i.Photos)
                .Include(f => f.Item)
                    .ThenInclude(i => i.Category)
                .Include(f => f.Item)
                    .ThenInclude(i => i.Owner)
                .Include(f => f.Item)
                    .ThenInclude(i => i.Reviews)
                .Where(f => f.UserId == userId);


            return await query.ToPagedResultAsync(request);

        }

        public async Task<UserFavoriteItem?> GetAsync(string userId, int itemId)
        {
            return await _context.UserFavoriteItems
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ItemId == itemId);
        }

        public async Task<bool> ExistsAsync(string userId, int itemId)
        {
            return await _context.UserFavoriteItems
                .AnyAsync(f => f.UserId == userId && f.ItemId == itemId);
        }

        public async Task AddAsync(UserFavoriteItem favorite)
        {
            await _context.UserFavoriteItems.AddAsync(favorite);
        }

        public async Task<List<string>> GetUsersToNotifyAsync(int itemId)
        {
            return await _context.UserFavoriteItems
                .Where(f => f.ItemId == itemId && f.NotifyWhenAvailable)
                .Select(f => f.UserId)
                .ToListAsync();
        }

        public async Task<List<UserFavoriteItem>> GetAllByItemIdAsync(int itemId)
        {
            return await _context.UserFavoriteItems
                .Where(f => f.ItemId == itemId)
                .ToListAsync();
        }

        public void Remove(UserFavoriteItem favorite)
        {
            _context.UserFavoriteItems.Remove(favorite);
        }


        public void RemoveRange(List<UserFavoriteItem> favorites)
        {
            _context.UserFavoriteItems.RemoveRange(favorites);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

    }
}
