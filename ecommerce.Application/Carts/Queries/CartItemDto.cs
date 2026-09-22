namespace ecommerce.Application.Carts.Queries;

public class CartItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public required string ProductName { get; set; }
    public required string ProductCode { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public int AvailableQuantity { get; set; }
    public decimal TotalPrice { get; init; }
}