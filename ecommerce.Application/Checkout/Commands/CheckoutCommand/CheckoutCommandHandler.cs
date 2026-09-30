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

        await using var transaction = await context.BeginTransactionAsync(cancellationToken);

        var order = new OrderEntity { UserId = currentUser.UserId };

        context.Orders.Add(order);

        // Fixed lock order (by ProductId) so concurrent checkouts always lock product rows
        // in the same sequence, preventing deadlocks.
        foreach (var item in itemsInCart.OrderBy(i => i.ProductId))
        {
            var productAffectedRow = await context.Products
                .Where(p => p.Id == item.ProductId && p.AvailableQuantity >= item.Quantity)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(p => p.AvailableQuantity, p => p.AvailableQuantity - item.Quantity)
                        .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), cancellationToken);

            if (productAffectedRow == 0)
                throw new InsufficientStockException(item.Product!.Name, item.Product.AvailableQuantity);

            context.OrderItems.Add(
                new OrderItemEntity
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    PriceAtPurchased = item.Product!.Price
                }
            );
        }

        var walletAffectedRow = await context.Wallets
            .Where(w => w.Id == wallet.Id && w.Balance >= totalPriceForOrder)
            .ExecuteUpdateAsync(w =>
                w.SetProperty(x => x.Balance, x => x.Balance - totalPriceForOrder)
                    .SetProperty(x => x.UpdatedAt, DateTime.UtcNow), cancellationToken);

        if (walletAffectedRow == 0) throw new InsufficientBalanceException(totalPriceForOrder, wallet.Balance);

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
        await transaction.CommitAsync(cancellationToken);

        // Enqueue email AFTER commit — never inside the transaction
        var user = await context.Users.FirstAsync(u => u.Id == currentUser.UserId, cancellationToken);
        await emailJobQueue.PushToQueue(EmailTypeEnum.Checkout, order.Id, user.UserName, cancellationToken);

        var freshBalance = await context.Wallets
            .Where(w => w.Id == wallet.Id)
            .Select(w => w.Balance)
            .FirstAsync(cancellationToken);

        return new CheckoutDto(order.Id, totalPriceForOrder, freshBalance);
    }
}