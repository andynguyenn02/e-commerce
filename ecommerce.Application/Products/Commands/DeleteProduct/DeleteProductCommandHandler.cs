using ecommerce.Application.Common.Exceptions;
using ecommerce.Application.Common.Interfaces;
using MediatR;

namespace ecommerce.Application.Products.Commands.DeleteProduct;

public class DeleteProductCommandHandler(IAppDbContext appDbContext) : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product =  await appDbContext.Products.FindAsync([request.ProductId], cancellationToken)
            ?? throw new NotFoundException("Product");
        
        product.IsDeleted  = true;
        product.UpdatedAt = DateTime.UtcNow;
        
        await appDbContext.SaveChangesAsync(cancellationToken);
    }
}