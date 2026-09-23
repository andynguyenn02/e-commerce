using ecommerce.Application.Common.Extensions;
using ecommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Carts.Commands.ClearCart;

public class ClearCartCommandHandler(IAppDbContext context, ICurrentUser currentUser)
    : IRequestHandler<ClearCartCommand>
{
    public async Task Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await context.GetOrCreateCart(currentUser.UserId, cancellationToken);

        var cartItems = await context.CartItems.Where(i => i.CartId == cart.Id).ToListAsync(cancellationToken);

        context.CartItems.RemoveRange(cartItems);
        await context.SaveChangesAsync(cancellationToken);
    }
}