namespace ecommerce.Domain.Entities;

public class CartEntity : CommonEntity
{
    public required Guid UserId { get; set; }
    public required UserEntity User { get; set; }
}