using MediatR;

namespace ecommerce.Application.Checkout.Commands.CheckoutCommand;

public record CheckoutDto(Guid OrderId, decimal TotalAmount, decimal RemainingBalance);

public record CheckoutCommand(List<Guid> CartItemId) : IRequest<CheckoutDto>;