using MediatR;

namespace ecommerce.Application.Products.Commands.UpdateProduct;

public record UpdateProductDto(
    string Name, decimal Price, string Code, int AvailableQuantity, Guid CategoryId);

public record UpdateProductCommand(Guid ProductId, UpdateProductDto Product) : IRequest;