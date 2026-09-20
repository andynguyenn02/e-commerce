namespace ecommerce.Domain.Entities;

public class CartEntity : CommonEntity
{
    public required Guid UserId { get; set; }
    public UserEntity? User { get; set; }
}