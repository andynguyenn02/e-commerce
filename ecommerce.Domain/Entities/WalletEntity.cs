namespace ecommerce.Domain.Entities;

public class WalletEntity : CommonEntity
{
    public decimal Balance { get; set; } = 1000;

    public required Guid UserId { get; set; }
    public UserEntity? User { get; set; }

    public bool IsSufficientBalance(decimal amount)
    {
        return Balance >= amount;
    }

    public void WithDraw(decimal amount)
    {
        Balance -= amount;
    }
}