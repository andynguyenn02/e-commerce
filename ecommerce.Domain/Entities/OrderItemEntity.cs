namespace ecommerce.Domain.Entities;

public class OrderItemEntity : CommonEntity
{
    public required Guid OrderId { get; set; }
    public OrderEntity? Order { get; set; }
    
    public required Guid ProductId { get; set; }
    public ProductEntity? Product { get; set; }
    
    public required decimal PriceAtPurchased { get; set; }
    public required int Quantity { get; set; }
}