using ecommerce.Application.Common.Interfaces;
using ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Common.Extensions;

public static class CartExtension
{
    public static async Task<CartEntity> GetOrCreateCart(this IAppDbContext context, Guid userId,
        CancellationToken cancellationToken)
    {
        var cart = await context.Carts.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart is not null) return cart;

        var newCart = new CartEntity
        {
            UserId = userId
        };

        context.Carts.Add(newCart);
        await context.SaveChangesAsync(cancellationToken);

        return newCart;
    }
}