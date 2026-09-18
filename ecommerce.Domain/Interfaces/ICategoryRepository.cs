using ecommerce.Domain.Entities;

namespace ecommerce.Domain.Interfaces;

public interface ICategoryRepository
{
    public Task<List<CategoryEntity>> GetAllCategories();
    public Task<CategoryEntity?> GetCategoryById(Guid id);
    public Task<CategoryEntity> CreateCategory(CategoryEntity category);
    public Task UpdateCategory(CategoryEntity category);
    public Task DeleteCategory(CategoryEntity category);
}