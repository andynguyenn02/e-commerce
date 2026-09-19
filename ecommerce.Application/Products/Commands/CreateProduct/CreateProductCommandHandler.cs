using ecommerce.Domain.Entities;
using ecommerce.Domain.Interfaces;
using MediatR;
namespace ecommerce.Application.Products.Commands.CreateProduct;

public class CreateProductCommandHandler(
    IProductRepository productRepository, ICategoryRepository categoryRepository, IUnitOfWork unitOfWork) : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetCategoryById(request.CategoryId, cancellationToken) 
                       ?? throw new KeyNotFoundException("Category not available");

        var product = new ProductEntity()
        {
            Id = Guid.CreateVersion7(),
            Name = request.Name,
            Price = request.Price,
            Code = request.Code,
            AvailableQuantity = request.AvailableQuantity,
            CategoryId = request.CategoryId,
            Category = category,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        await productRepository.CreateProduct(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return product.Id;
    }
}