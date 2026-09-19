namespace ecommerce.Domain.Entities;

public class CommonEntity
{
    public required Guid Id { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime UpdatedAt { get; set; }
}