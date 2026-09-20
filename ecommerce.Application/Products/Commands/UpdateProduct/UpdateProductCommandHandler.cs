using ecommerce.Application.Common.Interfaces;
using MediatR;

namespace ecommerce.Application.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler(IAppDbContext appDbContext)
: IRequestHandler<UpdateProductCommand>
{
    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var category = await appDbContext.Categories.FindAsync([request.Product.CategoryId], cancellationToken) 
                       ?? throw new KeyNotFoundException("Category not available");

        var product = await appDbContext.Products.FindAsync([request.ProductId], cancellationToken) 
            ?? throw new KeyNotFoundException("Product not found");
        
        product.Name = request.Product.Name;
        product.Price = request.Product.Price;
        product.Code = request.Product.Code;
        product.AvailableQuantity = request.Product.AvailableQuantity;
        product.CategoryId = category.Id;
        product.UpdatedAt = DateTime.UtcNow;
        
        await appDbContext.SaveChangesAsync(cancellationToken);
    }
}