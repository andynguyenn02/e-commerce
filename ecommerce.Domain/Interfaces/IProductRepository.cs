using ecommerce.Domain.Entities;

namespace ecommerce.Domain.Interfaces;

public interface IProductRepository
{
    Task<List<ProductEntity>> GetAllProducts(CancellationToken ct = default);
    Task<ProductEntity?> GetProductById(Guid id, CancellationToken ct = default);
    Task<ProductEntity> CreateProduct(ProductEntity product, CancellationToken ct = default);
    Task UpdateProduct(ProductEntity product, CancellationToken ct = default);
    Task DeleteProduct(ProductEntity product, CancellationToken ct = default);
    Task<List<ProductEntity>> GetProductsByCategory(CategoryEntity category, CancellationToken ct = default);
}