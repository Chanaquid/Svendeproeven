using backend.Common;
using backend.Dtos;
using backend.Interfaces;
using backend.Models;

namespace backend.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        //Get all category (if admin -> see all | if user -> see only active categories)
        public async Task<List<CategoryDto>> GetAllAsync(bool isAdmin = false)
        {
            var categoriesWithCount = await _categoryRepository.GetAllWithCountsAsync(isAdmin);

             return categoriesWithCount.Select(x => new CategoryDto
             {
                Id = x.Category.Id,
                Name = x.Category.Name,
                Slug = x.Category.Slug,
                Icon = x.Category.Icon,
                IsActive = x.Category.IsActive,
                ItemCount = x.ItemCount,
                CreatedAt = x.Category.CreatedAt, 
                UpdatedAt = x.Category.UpdatedAt 
             }).ToList();

        }

        //Get a category by id
        public async Task<CategoryDto> GetByIdAsync(int id, bool isAdmin = false)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                throw new KeyNotFoundException($"Category {id} not found.");

            if (!isAdmin && !category.IsActive)
                throw new KeyNotFoundException($"Category {id} not found.");

            // Map single category to DTO with item count
            var itemCount = await _categoryRepository.GetItemCountAsync(category.Id);
            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Icon = category.Icon,
                IsActive = category.IsActive,
                ItemCount = itemCount,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt  
            };
        }

        //Get a category by slug
        public async Task<CategoryDto> GetBySlugAsync(string slug)
        {
            var category = await _categoryRepository.GetBySlugAsync(slug);
            if (category == null || !category.IsActive)
                throw new KeyNotFoundException($"Category with slug '{slug}' not found.");

            return await MapToCategoryDto(category);
        }

        //Create a category
        public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
        {

            var slug = dto.Name.ToSlug(); //Auto-generated slug

            //Name and slug must be unique
            if (await _categoryRepository.ExistsByNameAsync(dto.Name.Trim()))
                throw new ArgumentException($"A category named '{dto.Name}' already exists.");

            if (await _categoryRepository.ExistsBySlugAsync(slug))
                throw new ArgumentException($"The generated slug '{slug}' is already in use.");

            var category = new Category
            {
                Name = dto.Name.Trim(),
                Slug = slug,
                Icon = dto.Icon?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _categoryRepository.AddAsync(category);
            await _categoryRepository.SaveChangesAsync();

            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Icon = category.Icon,
                IsActive = category.IsActive,
                ItemCount = 0,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };
        }

        //Update category
        public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryDto dto)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                throw new KeyNotFoundException($"Category {id} not found.");

            var newSlug = dto.Name.ToSlug();

            //Check name uniqueness — only if the name is actually changing
            if (!string.Equals(category.Name, dto.Name.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                if (await _categoryRepository.ExistsByNameAsync(dto.Name.Trim()))
                    throw new ArgumentException($"A category named '{dto.Name}' already exists.");
            }
            //Check slug uniqueness
            if (!string.Equals(category.Slug, newSlug, StringComparison.OrdinalIgnoreCase))
            {
                if (await _categoryRepository.ExistsBySlugAsync(newSlug))
                    throw new ArgumentException($"The generated slug '{newSlug}' is already in use.");
            }


            category.Name = dto.Name.Trim();
            category.Slug = newSlug;
            category.Icon = dto.Icon?.Trim();
            category.IsActive = dto.IsActive;

            _categoryRepository.Update(category);
            await _categoryRepository.SaveChangesAsync();

            return await MapToCategoryDto(category);
        }

        //Admin enables/disables a category
        public async Task ToggleActiveAsync(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                throw new KeyNotFoundException($"Category {id} not found.");

            category.IsActive = !category.IsActive;

            _categoryRepository.Update(category);
            await _categoryRepository.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                throw new KeyNotFoundException($"Category not found.");

            var count = await _categoryRepository.GetItemCountAsync(id);
            if (count > 0)
                throw new InvalidOperationException("Cannot delete a category with items.");

            _categoryRepository.Delete(category);
            await _categoryRepository.SaveChangesAsync();
        }

        //maptoDto
        private async Task<CategoryDto> MapToCategoryDto(Category c)
        {
            return new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Icon = c.Icon,
                IsActive = c.IsActive,
                //Only count items that are approved and active
                ItemCount = await _categoryRepository.GetItemCountAsync(c.Id),
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt  
            };
        }
    }
}