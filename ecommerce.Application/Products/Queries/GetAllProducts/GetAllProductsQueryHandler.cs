using ecommerce.Application.Common.Extensions;
using ecommerce.Application.Common.Interfaces;
using ecommerce.Application.Common.Models;
using MediatR;

namespace ecommerce.Application.Products.Queries.GetAllProducts;

public class GetAllProductsQueryHandler(IAppDbContext appDbContext)
    : IRequestHandler<GetAllProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        var products = appDbContext.Products.OrderBy(p => p.CreatedAt).Select(p =>
            new ProductDto(p.Id, p.Name, p.Price, p.Code, p.AvailableQuantity, p.CategoryId, p.CreatedAt));

        return await products.ToPageResultAsync(request.PageNumber, request.PageSize, cancellationToken);
    }
}