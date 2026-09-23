namespace ecommerce.Application.Orders.Queries;

public record OrderDetailDto(Guid OrderId, DateTime OrderDate, List<OrderItemDto> Items, decimal TotalAmount);