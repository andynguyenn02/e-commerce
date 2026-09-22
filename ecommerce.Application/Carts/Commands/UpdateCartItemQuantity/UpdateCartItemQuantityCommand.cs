using MediatR;

namespace ecommerce.Application.Carts.Commands.UpdateCartItemQuantity;

public record UpdateCartItemDto(int Quantity);

public record UpdateCartItemQuantityCommand(Guid CartItemId, UpdateCartItemDto Dto) : IRequest;