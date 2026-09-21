using ecommerce.Application.Common.Interfaces;
using MediatR;

namespace ecommerce.Application.Products.Commands.UpdatePrice;

public class UpdatePriceCommandHandler(IAppDbContext context) : IRequestHandler<UpdatePriceCommand, Guid>
{
    public async Task<Guid> Handle(UpdatePriceCommand request, CancellationToken cancellationToken)
    {
        var product = await context.Products.FindAsync([request.ProductId], cancellationToken)
                      ?? throw new KeyNotFoundException("Product not found");

        product.Price = request.Dto.Price;

        await context.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}