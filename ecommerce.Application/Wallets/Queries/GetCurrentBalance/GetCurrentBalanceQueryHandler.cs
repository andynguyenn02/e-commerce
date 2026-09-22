using ecommerce.Application.Common.Interfaces;
using ecommerce.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Wallets.Queries.GetCurrentBalance;

public class GetCurrentBalanceQueryHandler(IAppDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetCurrentBalanceQuery, WalletDto>
{
    public async Task<WalletDto> Handle(GetCurrentBalanceQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId) throw new UnauthorizedAccessException("Unauthorized");

        var wallet = await context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

        if (wallet is not null) return new WalletDto(wallet.Id, wallet.Balance);

        var newWallet = new WalletEntity
        {
            UserId = userId,
            Balance = 1000
        };

        context.Wallets.Add(newWallet);
        await context.SaveChangesAsync(cancellationToken);
        return new WalletDto(newWallet.UserId, newWallet.Balance);
    }
}