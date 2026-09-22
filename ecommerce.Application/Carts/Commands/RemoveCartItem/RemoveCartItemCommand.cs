using MediatR;

namespace ecommerce.Application.Carts.Commands.RemoveCartItem;

public record RemoveCartItemCommand(Guid CartItemId) : IRequest;