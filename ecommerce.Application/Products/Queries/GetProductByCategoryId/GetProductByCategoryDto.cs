namespace ecommerce.Application.Products.Queries.GetProductByCategoryId;

public record GetProductByCategoryDto(
    Guid Id,
    string Name,
    decimal Price,
    string Code,
    int AvailableQuantity,
    Guid CategoryId,
    DateTime CreatedAt);