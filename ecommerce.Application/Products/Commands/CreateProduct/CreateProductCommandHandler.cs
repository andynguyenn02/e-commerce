using ecommerce.Application.Common.Interfaces;
using ecommerce.Domain.Entities;
using MediatR;

namespace ecommerce.Application.Products.Commands.CreateProduct;

public class CreateProductCommandHandler(
    IAppDbContext appDbContext) : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var category = await appDbContext.Categories.FindAsync([request.CategoryId], cancellationToken)
                       ?? throw new KeyNotFoundException("Categories invalid");

        var product = new ProductEntity
        {
            Name = request.Name,
            Price = request.Price,
            Code = request.Code,
            AvailableQuantity = request.AvailableQuantity,
            CategoryId = request.CategoryId,
            IsDeleted = false
        };

        appDbContext.Products.Add(product);
        await appDbContext.SaveChangesAsync(cancellationToken);
        return product.Id;
    }
}