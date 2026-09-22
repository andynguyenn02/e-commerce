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
        var products = appDbContext.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
            products = products
                .Where(p => p.Name.Contains(request.Search) || p.Code.Contains(request.Search));

        if (request.CategoryId is not null)
            products = products
                .Where(p => p.CategoryId == request.CategoryId);

        var projection = products
            .OrderBy(p => p.CreatedAt)
            .Select(p =>
                new ProductDto(p.Id, p.Name, p.Price, p.Code, p.AvailableQuantity, p.CategoryId, p.CreatedAt));

        return await projection.ToPageResultAsync(request.PageNumber, request.PageSize, cancellationToken);
    }
}