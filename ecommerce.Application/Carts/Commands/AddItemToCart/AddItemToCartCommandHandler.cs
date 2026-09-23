using ecommerce.Application.Common.Exceptions;
using ecommerce.Application.Common.Extensions;
using ecommerce.Application.Common.Interfaces;
using ecommerce.Domain.Entities;
using MediatR;

namespace ecommerce.Application.Carts.Commands.AddItemToCart;

public class AddItemToCartCommandHandler(IAppDbContext context, ICurrentUser currentUser)
    : IRequestHandler<AddItemToCartCommand>
{
    public async Task Handle(AddItemToCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await context.GetOrCreateCart(currentUser.UserId, cancellationToken);

        var product = await context.Products.FindAsync([request.ProductId], cancellationToken);

        if (product == null) throw new KeyNotFoundException("Product not found");

        if (product.AvailableQuantity < request.Quantity)
            throw new BadRequestException("Not enough available quantity");


        context.CartItems.Add(new CartItemEntity
        {
            CartId = cart.Id,
            ProductId = product.Id,
            Quantity = request.Quantity
        });

        await context.SaveChangesAsync(cancellationToken);
    }
}