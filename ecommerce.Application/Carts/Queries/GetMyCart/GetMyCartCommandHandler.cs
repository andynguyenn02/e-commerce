using ecommerce.Application.Common.Extensions;
using ecommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Carts.Queries.GetMyCart;

public class GetMyCartCommandHandler(IAppDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetMyCartCommand, CartDto>
{
    public async Task<CartDto> Handle(GetMyCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await context.GetOrCreateCart(currentUser.UserId, cancellationToken);

        var cartItems = await context.CartItems
            .Where(ci => ci.CartId == cart.Id && ci.Product != null)
            .Select(ci => new CartItemDto
            {
                Id = ci.Id,
                ProductId = ci.ProductId,
                ProductName = ci.Product!.Name,
                Quantity = ci.Quantity,
                AvailableQuantity = ci.Product!.AvailableQuantity,
                ProductCode = ci.Product!.Code,
                UnitPrice = ci.Product!.Price,
                TotalPrice = ci.Product!.Price * ci.Quantity
            }).ToListAsync(cancellationToken);

        return new CartDto
        {
            CartItems = cartItems,
            TotalPrice = cartItems.Sum(ci => ci.TotalPrice),
            UserId = currentUser.UserId
        };
    }
}