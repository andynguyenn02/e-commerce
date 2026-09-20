using ecommerce.Application.Common.Interfaces;
using ecommerce.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Products.Queries.GetAllProducts;

public class GetAllProductsQueryHandler(IAppDbContext appDbContext) 
    : IRequestHandler<GetAllProductsQuery, List<ProductDto>>
{
    public async Task<List<ProductDto>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await appDbContext.Products.AsNoTracking().ToListAsync(cancellationToken);

        return [.. products.Select(p => new ProductDto(
            p.Id, p.Name, p.Price, p.Code, p.AvailableQuantity, p.CategoryId))];
    }
}
