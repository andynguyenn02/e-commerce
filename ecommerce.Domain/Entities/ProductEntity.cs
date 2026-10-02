namespace ecommerce.Domain.Entities;

public class ProductEntity : CommonEntity
{
    public required string Name { get; set; }
    public required decimal Price { get; set; }
    public required string Code { get; set; }
    public required int AvailableQuantity { get; set; }

    public required Guid CategoryId { get; set; }
    public CategoryEntity? Category { get; set; }

    public required bool IsDeleted { get; set; }

    public bool HasStockFor(int quantity)
    {
        return AvailableQuantity >= quantity;
    }

    public void SetPrice(decimal price)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);
        Price = price;
    }

    public void SetStock(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);
        AvailableQuantity = quantity;
    }

    public void RemoveStock(int quantity)
    {
        if (!HasStockFor(quantity))
            throw new InvalidOperationException($"Insufficient stock for {Name}");
        AvailableQuantity -= quantity;
    }
}