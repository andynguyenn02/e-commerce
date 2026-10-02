using MediatR;

namespace ecommerce.Application.Products.Commands.UpdatePrice;

public record UpdatePriceDto(decimal Price, decimal ExpectedPrice);

public record UpdatePriceCommand(Guid ProductId, UpdatePriceDto Dto) : IRequest<Guid>;
