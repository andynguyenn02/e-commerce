using MediatR;

namespace ecommerce.Application.Carts.Commands.ClearCart;

public record ClearCartCommand : IRequest;