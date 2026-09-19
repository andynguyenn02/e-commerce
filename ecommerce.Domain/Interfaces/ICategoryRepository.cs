using ecommerce.Domain.Entities;

namespace ecommerce.Domain.Interfaces;

public interface ICategoryRepository
{
    public Task<List<CategoryEntity>> GetAllCategories( CancellationToken ct = default);
    public Task<CategoryEntity?> GetCategoryById(Guid id, CancellationToken ct = default);
    public Task<CategoryEntity> CreateCategory(CategoryEntity category, CancellationToken ct = default);
    public Task UpdateCategory(CategoryEntity category, CancellationToken ct = default);
    public Task DeleteCategory(CategoryEntity category, CancellationToken ct = default);
}