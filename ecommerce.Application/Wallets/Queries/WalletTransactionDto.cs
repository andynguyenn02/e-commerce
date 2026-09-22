namespace ecommerce.Application.Wallets;

public record WalletTransactionDto(Guid WalletTransactionId, Guid OrderId, decimal TotalAmount, DateTime CreatedAt);