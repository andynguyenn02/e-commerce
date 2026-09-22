using MediatR;

namespace ecommerce.Application.Carts.Commands.AddItemToCart;

public record AddItemToCartCommand(Guid ProductId, int Quantity) : IRequest;