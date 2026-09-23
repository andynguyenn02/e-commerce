using ecommerce.Application.Common.Exceptions;
using ecommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Orders.Queries.GetOrderDetail;

public class GetOrderDetailQueryHandler(IAppDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetOrderDetailQuery, OrderDetailDto>
{
    public async Task<OrderDetailDto> Handle(GetOrderDetailQuery request, CancellationToken cancellationToken)
    {
        var order = await context.Orders.FindAsync([request.OrderId], cancellationToken);

        if (order == null) throw new NotFoundException("Order");

        if (order.UserId != currentUser.UserId) throw new NotFoundException("Order");

        var orderItems = await context.OrderItems
            .Where(oi => oi.OrderId == request.OrderId)
            .Include(oi => oi.Product)
            .Select(oi => new OrderItemDto
            {
                OrderItemId = oi.Id,
                ProductName = oi.Product!.Name,
                ProductCode = oi.Product!.Code,
                ProductPrice = oi.PriceAtPurchased,
                Quantity = oi.Quantity,
                TotalAmount = oi.PriceAtPurchased * oi.Quantity
            })
            .IgnoreQueryFilters() // for showing deleted item 
            .ToListAsync(cancellationToken);

        var totalAmount = orderItems.Sum(oi => oi.TotalAmount);

        var orderDetail = new OrderDetailDto(request.OrderId, order.CreatedAt, orderItems, totalAmount);

        return orderDetail;
    }
}