using ecommerce.Application.Common.Exceptions;
using ecommerce.Application.Common.Interfaces;
using ecommerce.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Checkout.Commands.CheckoutCommand;

public class CheckoutCommandHandler(IAppDbContext context, ICurrentUser currentUser)
    : IRequestHandler<CheckoutCommand, CheckoutDto>
{
    public async Task<CheckoutDto> Handle(
        CheckoutCommand request,
        CancellationToken cancellationToken
    )
    {
        var itemsInCart = await context
            .CartItems.Include(ci => ci.Product)
            .Where(ci =>
                request.CartItemId.Contains(ci.Id) && ci.Cart!.UserId == currentUser.UserId
            )
            .ToListAsync(cancellationToken);

        if (itemsInCart.Count == 0)
            throw new EmptyCartException();

        decimal totalPriceForOrder = 0;

        foreach (var item in itemsInCart)
        {
            if (item.Quantity > item.Product!.AvailableQuantity)
                throw new InsufficientStockException(
                    item.Product.Name,
                    item.Product.AvailableQuantity
                );

            totalPriceForOrder += item.Quantity * item.Product!.Price;
        }

        var wallet = await context.Wallets.FirstOrDefaultAsync(
            w => w.UserId == currentUser.UserId,
            cancellationToken
        );

        if (wallet is null)
            throw new NotFoundException("Wallet");

        if (wallet.Balance < totalPriceForOrder)
            throw new InsufficientBalanceException(totalPriceForOrder, wallet.Balance);

        var order = new OrderEntity { UserId = currentUser.UserId };

        context.Orders.Add(order);

        wallet.Balance -= totalPriceForOrder;

        foreach (var item in itemsInCart)
        {
            context.OrderItems.Add(
                new OrderItemEntity
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    PriceAtPurchased = item.Product!.Price,
                }
            );

            item.Product!.AvailableQuantity -= item.Quantity;
        }

        context.WalletTransactions.Add(
            new WalletTransactionEntity
            {
                OrderId = order.Id,
                WalletId = wallet.Id,
                Amount = -totalPriceForOrder,
            }
        );

        context.CartItems.RemoveRange(itemsInCart);

        await context.SaveChangesAsync(cancellationToken);

        return new CheckoutDto(order.Id, totalPriceForOrder, wallet.Balance);
    }
}
