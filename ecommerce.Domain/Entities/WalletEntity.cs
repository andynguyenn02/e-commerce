namespace ecommerce.Domain.Entities;

public class WalletEntity : CommonEntity
{
    public decimal Balance { get; set; } = 1000;
    
    public required Guid UserId { get; set; }
    public required UserEntity User { get; set; }
}