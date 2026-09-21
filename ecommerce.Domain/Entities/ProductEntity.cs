namespace ecommerce.Domain.Entities;

public class ProductEntity : CommonEntity
{
    public required string Name { get; set; }
    public required decimal Price { get; set; }
    public required string Code { get; set; }
    public required int AvailableQuantity { get; set; }

    public required Guid CategoryId { get; set; }
    public CategoryEntity? Category { get; set; }

    public required bool IsDeleted { get; set; }
}