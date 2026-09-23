using ecommerce.Application.Common.Interfaces;
using ecommerce.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Checkout.Commands.CheckoutCommand;

public class CheckoutCommandHandler(IAppDbContext context, ICurrentUser currentUser)
    : IRequestHandler<CheckoutCommand, CheckoutDto>
{
    public async Task<CheckoutDto> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        var carts = await context.Carts.FirstOrDefaultAsync(c => c.UserId == currentUser.UserId, cancellationToken);

        var itemsInCart = await context.CartItems
            .Include(ci => ci.Product)
            .Where(ci => request.CartItemId.Contains(ci.Id))
            .ToListAsync(cancellationToken);

        if (itemsInCart.Count == 0) throw new InvalidOperationException("No items found in cart");

        decimal totalPriceForOrder = 0;

        foreach (var item in itemsInCart)
        {
            if (item.Quantity > item.Product!.AvailableQuantity)
                throw new InvalidOperationException($"{item.Product.Name}: only {item.Product.AvailableQuantity} left");

            totalPriceForOrder += item.Quantity * item.Product!.Price;
        }

        var wallet =
            await context.Wallets.FirstOrDefaultAsync(w => w.UserId == currentUser.UserId, cancellationToken);

        if (wallet!.Balance < totalPriceForOrder)
            throw new InvalidOperationException($"Need {totalPriceForOrder}, have {wallet.Balance}");

        var order = new OrderEntity
        {
            UserId = currentUser.UserId
        };
        await using var transaction = await context.BeginTransactionAsync(cancellationToken);
        try
        {
            context.Orders.Add(order);

            wallet.Balance -= totalPriceForOrder;

            foreach (var item in itemsInCart)
            {
                context.OrderItems.Add(new OrderItemEntity
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    PriceAtPurchased = item.Product!.Price
                });

                item.Product!.AvailableQuantity -= item.Quantity;
            }

            context.WalletTransactions.Add(new WalletTransactionEntity
            {
                OrderId = order.Id,
                WalletId = wallet.Id,
                Amount = -totalPriceForOrder
            });

            context.CartItems.RemoveRange(itemsInCart);

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new CheckoutDto(order.Id, totalPriceForOrder, wallet.Balance);
        }

        catch (Exception e)
        {
            Console.WriteLine(e);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}