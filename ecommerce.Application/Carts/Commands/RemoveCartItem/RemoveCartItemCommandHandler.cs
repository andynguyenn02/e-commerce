using ecommerce.Application.Common.Exceptions;
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

        if (cartItem == null) throw new NotFoundException("Cart item");

        if (cartItem.CartId != userCart.Id) throw new NotFoundException("Cart item");

        context.CartItems.Remove(cartItem);

        await context.SaveChangesAsync(cancellationToken);
    }
}