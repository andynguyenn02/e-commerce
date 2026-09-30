namespace ecommerce.Domain.Entities;

public class WalletEntity : CommonEntity
{
    public decimal Balance { get; private set; } = 1000;

    public required Guid UserId { get; set; }
    public UserEntity? User { get; set; }

    public bool IsSufficientBalance(decimal amount)
    {
        return Balance >= amount;
    }

    public void Withdraw(decimal amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        if (!IsSufficientBalance(amount))
            throw new InvalidOperationException("Insufficient balance");
        Balance -= amount;
    }

    public void Deposit(decimal amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        Balance += amount;
    }
}
