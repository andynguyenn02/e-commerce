using ecommerce.Application.Common.Exceptions;
using ecommerce.Application.Common.Interfaces;
using MediatR;

namespace ecommerce.Application.Products.Queries.GetProductById;

public class GetProductByIdQueryHandler(IAppDbContext appDbContext)
    : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await appDbContext.Products.FindAsync([request.ProductId], cancellationToken)
                      ?? throw new NotFoundException("Product");

        return new ProductDto(
            product.Id, product.Name, product.Price,
            product.Code, product.AvailableQuantity, product.CategoryId, product.CreatedAt);
    }
}