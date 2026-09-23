using ecommerce.Application.Common.Exceptions;
using ecommerce.Application.Common.Extensions;
using ecommerce.Application.Common.Interfaces;
using MediatR;

namespace ecommerce.Application.Carts.Commands.UpdateCartItemQuantity;

public class UpdateCartItemQuantityCommandHandler(IAppDbContext context, ICurrentUser currentUser)
    : IRequestHandler<UpdateCartItemQuantityCommand>
{
    public async Task Handle(UpdateCartItemQuantityCommand request, CancellationToken cancellationToken)
    {
        var userCart = await context.GetOrCreateCart(currentUser.UserId, cancellationToken);

        var cartItem = await context.CartItems.FindAsync([request.CartItemId], cancellationToken);

        if (cartItem == null) throw new NotFoundException("Cart item");

        var product = await context.Products.FindAsync([cartItem.ProductId], cancellationToken);

        if (cartItem.CartId != userCart.Id) throw new NotFoundException("Cart item");
        if (product is null) throw new NotFoundException("Product");

        if (product.AvailableQuantity < request.Dto.Quantity)
            throw new InsufficientStockException(product.Name, product.AvailableQuantity);


        cartItem.Quantity = request.Dto.Quantity;

        await context.SaveChangesAsync(cancellationToken);
    }
}