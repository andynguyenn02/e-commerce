using ecommerce.Application.Common.Interfaces;
using ecommerce.Domain.Interfaces;
using MediatR;

namespace ecommerce.Application.Products.Queries.GetProductById;

public class GetProductByIdQueryHandler(IAppDbContext appDbContext) 
    : IRequestHandler<GetProductByIdQuery, GetProductByIdDto>
{
    public async Task<GetProductByIdDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await appDbContext.Products.FindAsync([request.ProductId], cancellationToken)
            ?? throw new KeyNotFoundException("Product with this id not found");
        
        return new GetProductByIdDto(
            product.Id, product.Name, product.Price,
            product.Code, product.AvailableQuantity, product.CategoryId);
    }
}
