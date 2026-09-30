using ecommerce.Application.Common.Exceptions;
using ecommerce.Application.Common.Interfaces;
using ecommerce.Domain.Entities;
using ecommerce.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Checkout.Commands.CheckoutCommand;

public class CheckoutCommandHandler(
    IAppDbContext context,
    ICurrentUser currentUser,
    IEmailJobQueue emailJobQueue)
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

        var unavailableIds = itemsInCart.Where(i => i.Product is null).Select(i => i.ProductId).ToList();

        if (unavailableIds.Count != 0)
        {
            var names = await context.Products.IgnoreQueryFilters()
                .Where(p => unavailableIds.Contains(p.Id))
                .Select(p => p.Name)
                .ToListAsync(cancellationToken);

            throw new ProductNoLongerAvailableException(names);
        }

        decimal totalPriceForOrder = 0;

        foreach (var item in itemsInCart)
        {
            if (!item.Product!.HasStockFor(item.Quantity))
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

        if (!wallet.IsSufficientBalance(totalPriceForOrder))
            throw new InsufficientBalanceException(totalPriceForOrder, wallet.Balance);

        var order = new OrderEntity { UserId = currentUser.UserId };

        context.Orders.Add(order);

        wallet.WithDraw(totalPriceForOrder);

        foreach (var item in itemsInCart)
        {
            context.OrderItems.Add(
                new OrderItemEntity
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    PriceAtPurchased = item.Product!.Price
                }
            );

            item.Product!.RemoveStock(item.Quantity);
        }

        context.WalletTransactions.Add(
            new WalletTransactionEntity
            {
                OrderId = order.Id,
                WalletId = wallet.Id,
                Amount = -totalPriceForOrder
            }
        );

        context.CartItems.RemoveRange(itemsInCart);

        await context.SaveChangesAsync(cancellationToken);

        // Enqueue email AFTER commit — never inside the transaction
        var user = await context.Users.FirstAsync(u => u.Id == currentUser.UserId, cancellationToken);
        await emailJobQueue.PushToQueue(EmailTypeEnum.Checkout, order.Id, user.UserName, cancellationToken);

        return new CheckoutDto(order.Id, totalPriceForOrder, wallet.Balance);
    }
}