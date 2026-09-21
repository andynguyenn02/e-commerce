using ecommerce.Application.Common.Interfaces;
using ecommerce.Application.Common.Models;
using MediatR;

namespace ecommerce.Application.Products.Queries.GetAllProducts;

public record GetAllProductsQuery : IRequest<PagedResult<ProductDto>>, IPagedQuery
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}