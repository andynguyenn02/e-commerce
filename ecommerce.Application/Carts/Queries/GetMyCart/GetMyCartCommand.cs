using MediatR;

namespace ecommerce.Application.Carts.Queries.GetMyCart;

public record GetMyCartCommand : IRequest<CartDto>;