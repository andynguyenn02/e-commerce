using ecommerce.Domain.Interfaces;
using MediatR;

namespace ecommerce.Application.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler(IProductRepository productRepository, ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
: IRequestHandler<UpdateProduct>
{
    public async Task Handle(UpdateProduct request, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetCategoryById(request.ProductDto.CategoryId, cancellationToken) 
                       ?? throw new KeyNotFoundException("Category not available");

        var product = await productRepository.GetProductById(request.ProductId, cancellationToken) 
            ?? throw new KeyNotFoundException("Product not found");
        
        product.Name = request.ProductDto.Name;
        product.Price = request.ProductDto.Price;
        product.Code = request.ProductDto.Code;
        product.AvailableQuantity = request.ProductDto.AvailableQuantity;
        product.CategoryId = category.Id;
        product.Category = category;
        product.UpdatedAt = DateTime.UtcNow;
        
        await productRepository.UpdateProduct(product, cancellationToken);
        
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}