using MediatR;

namespace ecommerce.Application.Wallets.Queries.GetCurrentBalance;

public record GetCurrentBalanceQuery : IRequest<WalletDto>;