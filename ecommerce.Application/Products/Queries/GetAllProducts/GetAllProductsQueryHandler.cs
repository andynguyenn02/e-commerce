using ecommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Products.Queries.GetAllProducts;

public class GetAllProductsQueryHandler(IAppDbContext appDbContext)
    : IRequestHandler<GetAllProductsQuery, List<ProductDto>>
{
    public async Task<List<ProductDto>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await appDbContext.Products.Select(p =>
                new ProductDto(p.Id, p.Name, p.Price, p.Code, p.AvailableQuantity, p.CategoryId, p.CreatedAt))
            .ToListAsync(cancellationToken);

        return products;
    }
}