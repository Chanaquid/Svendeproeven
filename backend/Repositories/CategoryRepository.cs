using backend.Data;
using backend.Dtos;
using backend.Helpers;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public CategoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        //Get all category for admin | active only for users
        public async Task<List<Category>> GetAllAsync(bool isAdmin = false)
        {
            var query = _context.Categories.AsNoTracking(); //optimized for read

            if (!isAdmin)
            {
                query = query.Where(c => c.IsActive);
            }    

            return await query.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            return await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Category?> GetByNameAsync(string name)
        {
            return await _context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
        }

        //Get category by slug
        public async Task<Category?> GetBySlugAsync(string slug)
        {
            return await _context.Categories
                .FirstOrDefaultAsync(c => c.Slug.ToLower() == slug.ToLower());
        }

        //Check if category exists
        public async Task<bool> ExistsByNameAsync(string name)
        {
            return await _context.Categories.AnyAsync(c => c.Name.ToLower() == name.ToLower());
        }

        //Check if slug exists
        public async Task<bool> ExistsBySlugAsync(string slug)
        {
            return await _context.Categories.AnyAsync(c => c.Slug.ToLower() == slug.ToLower());
        }

        public async Task AddAsync(Category category)
        {
            await _context.Categories.AddAsync(category);
        }

        public void Update(Category category)
        {
            category.UpdatedAt = DateTime.UtcNow;
            _context.Categories.Update(category);
        }

        public void Delete(Category category)
        {
            _context.Categories.Remove(category);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }


        public async Task<int> GetItemCountAsync(int categoryId)
        {
            return await _context.Items
                .CountAsync(i => i.CategoryId == categoryId && i.IsActive && i.Status == ItemStatus.Approved);
        }

        public async Task<CategoryWithCount?> GetByIdWithCountAsync(int id)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return null;

            var itemCount = await _context.Items
                .CountAsync(i => i.CategoryId == id
                    && i.IsActive
                    && i.Status == ItemStatus.Approved);

            return new CategoryWithCount
            {
                Category = category,
                ItemCount = itemCount
            };
        }


        public async Task<List<CategoryWithCount>> GetAllWithCountsAsync(bool isAdmin = false)
        {
            var query = _context.Categories.AsNoTracking();

            if (!isAdmin)
                query = query.Where(c => c.IsActive);

            return await query
                .Select(c => new CategoryWithCount
                {
                    Category = c,
                    ItemCount = _context.Items.Count(i => i.CategoryId == c.Id
                        && i.IsActive
                        && i.Status == ItemStatus.Approved)
                })
                .OrderBy(x => x.Category.Name)
                .ToListAsync();
        }
    
    }
}