using ecommerce.Domain.Entities;

namespace ecommerce.Domain.Interfaces;

public interface IOrderRepository
{
    public Task<List<OrderEntity>> GetAllOrdersByUserId(Guid userId);
    public Task<OrderEntity?> GetOrderById(Guid id);
    public Task<OrderEntity> CreateOrder(OrderEntity order);
    public Task UpdateOrder(OrderEntity order);
    public Task DeleteOrder(OrderEntity order);
    public Task<List<OrderEntity>> GetOrderByUserId(Guid userId);
    
    public Task<OrderItemEntity> CreateOrderItem(Guid orderId, OrderItemEntity orderItem);
    public Task<List<OrderItemEntity>> GetOrderItemByOrderId(Guid id);
    
}