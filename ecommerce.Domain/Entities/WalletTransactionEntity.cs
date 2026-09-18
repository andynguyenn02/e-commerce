namespace ecommerce.Domain.Entities;

public class WalletTransactionEntity : CommonEntity
{
    public required Guid WalletId { get; set; }
    public required WalletEntity Wallet { get; set; }
    
    public required Guid OrderId { get; set; }
    public required OrderEntity Order { get; set; }
    
    public required decimal Amount { get; set; }
}