using backend.Dtos;

namespace backend.Interfaces
{
    public interface ICategoryService
    {
        Task<List<CategoryDto>> GetAllAsync(bool isAdmin = false);
        Task<CategoryDto> GetByIdAsync(int id, bool isAdmin = false);
        Task<CategoryDto> GetBySlugAsync(string slug);
        Task<CategoryDto> CreateAsync(CreateCategoryDto dto);
        Task<CategoryDto> UpdateAsync(int id, UpdateCategoryDto dto);
        Task ToggleActiveAsync(int id); //Admin enables/disables a category
        Task DeleteAsync(int id);
    }
}