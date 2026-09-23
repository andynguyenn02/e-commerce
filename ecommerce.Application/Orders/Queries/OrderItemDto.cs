namespace ecommerce.Application.Orders.Queries;

public record OrderItemDto
{
    public Guid OrderItemId { get; set; }
    public string ProductName { get; set; }
    public string ProductCode { get; set; }
    public decimal ProductPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
}