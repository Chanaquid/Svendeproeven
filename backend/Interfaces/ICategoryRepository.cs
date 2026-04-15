using backend.Dtos;
using backend.Helpers;
using backend.Models;

namespace backend.Interfaces
{
    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllAsync(bool isAdmin = false);
        Task<Category?> GetByIdAsync(int id);
        Task<Category?> GetByNameAsync(string name);
        Task<Category?> GetBySlugAsync(string slug);
        Task<bool> ExistsByNameAsync(string name);
        Task<bool> ExistsBySlugAsync(string slug);
        Task AddAsync(Category category);
        void Update(Category category);
        void Delete(Category category);
        Task SaveChangesAsync();

        //counts
        Task<int> GetItemCountAsync(int categoryId);
        Task<CategoryWithCount?> GetByIdWithCountAsync(int id);
        Task<List<CategoryWithCount>> GetAllWithCountsAsync(bool isAdmin = false);
    }
}