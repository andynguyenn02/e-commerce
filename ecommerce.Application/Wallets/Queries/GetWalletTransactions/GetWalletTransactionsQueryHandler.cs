using ecommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Wallets.Queries.GetWalletTransactions;

public class GetWalletTransactionsQueryHandler(IAppDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetWalletTransactionsQuery, List<WalletTransactionDto>>
{
    public async Task<List<WalletTransactionDto>> Handle(GetWalletTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var wallet = await context.Wallets.FirstOrDefaultAsync(w => w.UserId == currentUser.UserId, cancellationToken);

        if (wallet is null) throw new KeyNotFoundException("Wallet not found");

        var walletTransactions = await context.WalletTransactions.Where(wt => wt.WalletId == wallet.Id)
            .Select(wt => new WalletTransactionDto(wt.Id, wt.OrderId, wt.Amount, wt.CreatedAt))
            .ToListAsync(cancellationToken);

        return walletTransactions;
    }
}