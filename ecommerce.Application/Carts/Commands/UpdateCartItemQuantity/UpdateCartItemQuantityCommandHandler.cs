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
        if (currentUser.UserId is not { } userId) throw new UnauthorizedAccessException("Unauthorized");
        var userCart = await context.GetOrCreateCart(userId, cancellationToken);

        var cartItem = await context.CartItems.FindAsync([request.CartItemId], cancellationToken);

        if (cartItem == null) throw new KeyNotFoundException("Cart item not found");

        var product = await context.Products.FindAsync([cartItem.ProductId], cancellationToken);

        if (cartItem.CartId != userCart.Id) throw new KeyNotFoundException("Cart not found");
        if (product is null) throw new KeyNotFoundException("Product not found");

        if (product.AvailableQuantity < request.Dto.Quantity)
            throw new BadRequestException("Not enough available quantity");


        cartItem.Quantity = request.Dto.Quantity;

        await context.SaveChangesAsync(cancellationToken);
    }
}