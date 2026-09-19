namespace ecommerce.Application.Products.Queries.GetProductById;

public record GetProductByIdDto( Guid Id,
    string Name,
    decimal Price,
    string Code,
    int AvailableQuantity,
    Guid CategoryId);