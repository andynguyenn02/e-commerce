using ecommerce.Domain.Interfaces;
using MediatR;

namespace ecommerce.Application.Products.Commands.DeleteProduct;

public class DeleteProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork) : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product =  await productRepository.GetProductById(request.ProductId, cancellationToken)
            ?? throw new KeyNotFoundException($"Product with id {request.ProductId} does not exist");
        
        await productRepository.DeleteProduct(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}