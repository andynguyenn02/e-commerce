using MediatR;

namespace ecommerce.Application.Wallets.Queries.GetWalletTransactions;

public record GetWalletTransactionsQuery : IRequest<List<WalletTransactionDto>>;