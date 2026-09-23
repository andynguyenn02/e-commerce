namespace ecommerce.Application.Orders.Queries;

public record OrderSummaryDto
{
    public Guid OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
}