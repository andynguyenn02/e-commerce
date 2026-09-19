using MediatR;

namespace ecommerce.Application.Products.Commands.CreateProduct;

public record CreateProductCommand(
    string Name, decimal Price, string Code, int AvailableQuantity, Guid CategoryId) : IRequest<Guid>;
    
    