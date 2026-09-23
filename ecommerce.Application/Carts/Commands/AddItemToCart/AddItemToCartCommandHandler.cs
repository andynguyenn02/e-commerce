using ecommerce.Application.Common.Exceptions;
using ecommerce.Application.Common.Extensions;
using ecommerce.Application.Common.Interfaces;
using ecommerce.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Carts.Commands.AddItemToCart;

public class AddItemToCartCommandHandler(IAppDbContext context, ICurrentUser currentUser)
    : IRequestHandler<AddItemToCartCommand>
{
    public async Task Handle(AddItemToCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await context.GetOrCreateCart(currentUser.UserId, cancellationToken);

        var product = await context.Products.FindAsync([request.ProductId], cancellationToken);

        if (product == null)
            throw new NotFoundException("Product");

        var cartItem = await context.CartItems
        .FirstOrDefaultAsync(
            ci => ci.CartId == cart.Id && ci.ProductId == product.Id,
            cancellationToken
        );

        var existingQuantity = cartItem?.Quantity ?? 0;

        if (product.AvailableQuantity < existingQuantity + request.Quantity)
            throw new InsufficientStockException(product.Name, product.AvailableQuantity);

        if (cartItem is null)
        {
            context.CartItems.Add(
                new CartItemEntity
                {
                    CartId = cart.Id,
                    ProductId = product.Id,
                    Quantity = request.Quantity,
                }
            );
        }
        else
        {
            cartItem.Quantity += request.Quantity;
        }
        await context.SaveChangesAsync(cancellationToken);
    }
}
