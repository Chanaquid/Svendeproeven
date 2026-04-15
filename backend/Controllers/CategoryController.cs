using backend.Dtos;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [Route("api/categories")]
    [ApiController]
    public class CategoryController : BaseController
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        //GET /api/categories
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetAll()
        {
            var caller = GetCallerOrNull();
            var result = await _categoryService.GetAllAsync(caller?.IsAdmin ?? false);
            return Ok(ApiResponse<List<CategoryDto>>.Ok(result));
        }

        //GET /api/categories/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> GetById(int id)
        {
            var caller = GetCallerOrNull();
            var result = await _categoryService.GetByIdAsync(id, caller?.IsAdmin ?? false);
            return Ok(ApiResponse<CategoryDto>.Ok(result));
        }

        //GET /api/categories/slug/{slug}
        [HttpGet("slug/{slug}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> GetBySlug(string slug)
        {
            var result = await _categoryService.GetBySlugAsync(slug);
            return Ok(ApiResponse<CategoryDto>.Ok(result));
        }

        // POST /api/categories
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> Create([FromBody] CreateCategoryDto dto)
        {
            var result = await _categoryService.CreateAsync(dto);
            return Ok(ApiResponse<CategoryDto>.Ok(result, "Category created successfully."));
        }

        // PUT /api/categories/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> Update(int id, [FromBody] UpdateCategoryDto dto)
        {
            var result = await _categoryService.UpdateAsync(id, dto);
            return Ok(ApiResponse<CategoryDto>.Ok(result, "Category updated successfully."));
        }

        // PATCH /api/categories/{id}/toggle
        [HttpPatch("{id}/toggle")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<string>>> Toggle(int id)
        {
            await _categoryService.ToggleActiveAsync(id);
            return Ok(ApiResponse<string>.Ok(null, "Category status toggled successfully."));
        }

        // DELETE /api/categories/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<string>>> Delete(int id)
        {
            await _categoryService.DeleteAsync(id);
            return Ok(ApiResponse<string>.Ok(null, "Category deleted successfully."));
        }
    }
}