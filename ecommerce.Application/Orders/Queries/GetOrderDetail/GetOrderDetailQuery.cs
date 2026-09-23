using MediatR;

namespace ecommerce.Application.Orders.Queries.GetOrderDetail;

public record GetOrderDetailQuery(Guid OrderId) : IRequest<OrderDetailDto>;