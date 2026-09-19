using MediatR;

namespace ecommerce.Application.Products.Queries.GetProductById;

public record GetProductByIdQuery(Guid ProductId) : IRequest<GetProductByIdDto>;