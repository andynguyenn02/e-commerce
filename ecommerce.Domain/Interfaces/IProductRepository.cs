using ecommerce.Domain.Entities;

namespace ecommerce.Domain.Interfaces;

public interface IProductRepository
{
    public Task<List<ProductEntity>> GetAllProducts();
    public Task<ProductEntity?> GetProductById(Guid id);
    public Task<ProductEntity> CreateProduct(ProductEntity product);
    public Task UpdateProduct(ProductEntity product);
    public Task DeleteProduct(ProductEntity product);
    public Task<List<ProductEntity>> GetProductsByCategory(Guid categoryId);
}