using ecommerce.Domain.Entities;

namespace ecommerce.Domain.Interfaces;

public interface ICartRepository
{
    public Task<CartEntity?> GetCartByUserId(Guid id);
    public Task<CartEntity> CreateCart(CartEntity cart);
    public Task UpdateCart(CartEntity cart);
    public Task DeleteCart(CartEntity cart);
    
    public Task<List<CartItemEntity>> GetCartItemByCartId(Guid id);
    public Task UpdateCartItem(CartItemEntity cart);
    public Task AddItemToCart(Guid cartId, CartItemEntity cartItem);
    public Task RemoveItemFromCart(Guid cartId, CartItemEntity cartItem);
    public Task ClearCartItemByCartId(Guid cartId);
}