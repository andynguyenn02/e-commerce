namespace ecommerce.Application.Carts.Queries.GetMyCart;

public class CartDto
{
    public Guid UserId { get; set; }
    public List<CartItemDto> CartItems { get; set; } = [];
    public decimal TotalPrice { get; set; }
}