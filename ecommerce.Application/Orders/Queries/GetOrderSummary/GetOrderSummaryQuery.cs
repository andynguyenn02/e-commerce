using MediatR;

namespace ecommerce.Application.Orders.Queries.GetOrderSummary;

public record GetOrderSummaryQuery : IRequest<List<OrderSummaryDto>>;