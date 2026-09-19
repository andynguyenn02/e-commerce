using MediatR;

namespace ecommerce.Application.Products.Commands.UpdateProduct;

public record UpdateProductDto(
    string Name,
    decimal Price,
    string Code,
    int AvailableQuantity,
    Guid CategoryId
);

public record UpdateProduct(Guid ProductId, UpdateProductDto ProductDto) : IRequest;