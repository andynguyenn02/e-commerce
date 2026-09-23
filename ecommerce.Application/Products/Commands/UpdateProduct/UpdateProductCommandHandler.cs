using ecommerce.Application.Common.Exceptions;
using ecommerce.Application.Common.Interfaces;
using MediatR;

namespace ecommerce.Application.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler(IAppDbContext appDbContext)
    : IRequestHandler<UpdateProductCommand>
{
    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var category = await appDbContext.Categories.FindAsync([request.Product.CategoryId], cancellationToken)
                       ?? throw new NotFoundException("Category");

        var product = await appDbContext.Products.FindAsync([request.ProductId], cancellationToken)
                      ?? throw new NotFoundException("Product");

        product.Name = request.Product.Name;
        product.Price = request.Product.Price;
        product.Code = request.Product.Code;
        product.AvailableQuantity = request.Product.AvailableQuantity;
        product.CategoryId = category.Id;

        await appDbContext.SaveChangesAsync(cancellationToken);
    }
}