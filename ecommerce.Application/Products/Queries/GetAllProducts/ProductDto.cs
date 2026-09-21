namespace ecommerce.Application.Products.Queries.GetAllProducts;

public record ProductDto(
    Guid Id,
    string Name,
    decimal Price,
    string Code,
    int AvailableQuantity,
    Guid CategoryId,
    DateTime CreatedAt
);