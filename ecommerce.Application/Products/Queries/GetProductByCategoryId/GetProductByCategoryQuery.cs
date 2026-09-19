using ecommerce.Application.Products.Queries.GetAllProducts;
using MediatR;

namespace ecommerce.Application.Products.Queries.GetProductByCategoryId;

public record GetProductByCategoryQuery(Guid CategoryId) : IRequest<List<GetProductByCategoryDto>>;