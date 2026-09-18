namespace ecommerce.Domain.Entities;

public class CartItemEntity : CommonEntity
{
    public required Guid CartId { get; set; }
    public required CartEntity Cart { get; set; }
    
    public required Guid ProductId { get; set; }
    public required ProductEntity Product { get; set; }
    
    public required int Quantity { get; set; }
}