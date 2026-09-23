using ecommerce.Application.Common.Extensions;
using ecommerce.Application.Common.Interfaces;
using MediatR;

namespace ecommerce.Application.Carts.Commands.RemoveCartItem;

public class RemoveCartItemCommandHandler(IAppDbContext context, ICurrentUser currentUser)
    : IRequestHandler<RemoveCartItemCommand>
{
    public async Task Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
    {
        var userCart = await context.GetOrCreateCart(currentUser.UserId, cancellationToken);

        var cartItem = await context.CartItems.FindAsync([request.CartItemId], cancellationToken);

        if (cartItem == null) throw new KeyNotFoundException("Cart item not found");

        if (cartItem.CartId != userCart.Id) throw new KeyNotFoundException("Cart not found");

        context.CartItems.Remove(cartItem);

        await context.SaveChangesAsync(cancellationToken);
    }
}