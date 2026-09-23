using ecommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Orders.Queries.GetOrderSummary;

public class GetOrderSummaryQueryHandler(IAppDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetOrderSummaryQuery, List<OrderSummaryDto>>
{
    public async Task<List<OrderSummaryDto>> Handle(GetOrderSummaryQuery request, CancellationToken cancellationToken)
    {
        var orders = await context.Orders
            .Where(o => o.UserId == currentUser.UserId)
            .Select(o => new OrderSummaryDto
            {
                OrderId = o.Id,
                OrderDate = o.CreatedAt,
                Quantity = context.OrderItems.Count(oi => oi.OrderId == o.Id),
                TotalAmount = context.OrderItems.Where(oi => oi.OrderId == o.Id).Sum(oi => oi.PriceAtPurchased)
            })
            .OrderByDescending(o => o.OrderDate)
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        return orders;
    }
}