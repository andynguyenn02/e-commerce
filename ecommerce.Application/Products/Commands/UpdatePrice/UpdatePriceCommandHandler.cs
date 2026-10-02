using ecommerce.Application.Common.Exceptions;
using ecommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Products.Commands.UpdatePrice;

public class UpdatePriceCommandHandler(IAppDbContext context)
    : IRequestHandler<UpdatePriceCommand, Guid>
{
    public async Task<Guid> Handle(UpdatePriceCommand request, CancellationToken cancellationToken)
    {
        var affectedRows = await context.Products
            .Where(p => p.Id == request.ProductId && p.Price == request.Dto.ExpectedPrice)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(p => p.Price, request.Dto.Price)
                    .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), cancellationToken);

        if (affectedRows == 0)
        {
            var exists = await context.Products.AnyAsync(p => p.Id == request.ProductId, cancellationToken);

            if (exists)
                throw new PriceChangedException();
            else
                throw new NotFoundException("Product");
        }

        return request.ProductId;
    }
}
