using MediatR;

namespace ecommerce.Application.Products.Commands.UpdatePrice;

public record UpdatePriceDto(decimal Price);

public record UpdatePriceCommand(Guid ProductId, UpdatePriceDto Dto) : IRequest<Guid>;