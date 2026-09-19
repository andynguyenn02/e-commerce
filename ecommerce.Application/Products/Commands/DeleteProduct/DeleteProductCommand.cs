using MediatR;

namespace ecommerce.Application.Products.Commands.DeleteProduct;

public record DeleteProductCommand(Guid ProductId): IRequest;