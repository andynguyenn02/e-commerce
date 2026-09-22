namespace ecommerce.Application.Products.Queries;

public record ProductDto(
    Guid Id,
    string Name,
    decimal Price,
    string Code,
    int AvailableQuantity,
    Guid CategoryId,
    DateTime CreatedAt
);