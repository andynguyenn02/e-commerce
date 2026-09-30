namespace ecommerce.Domain.Entities;

public class CartItemEntity : CommonEntity
{
    public required Guid CartId { get; set; }
    public CartEntity? Cart { get; set; }
    
    public required Guid ProductId { get; set; }
    public ProductEntity? Product { get; set; }
    
    public required int Quantity { get; set; }

    public void SetQuantity(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        Quantity = quantity;
    }

    public void AddQuantity(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        Quantity += quantity;
    }
}