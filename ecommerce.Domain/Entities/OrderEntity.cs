namespace ecommerce.Domain.Entities;

public class OrderEntity : CommonEntity
{
    public required Guid UserId { get; set; }
    public required UserEntity User { get; set; }
    public DateTime EmailSentAt { get; set; }    
}