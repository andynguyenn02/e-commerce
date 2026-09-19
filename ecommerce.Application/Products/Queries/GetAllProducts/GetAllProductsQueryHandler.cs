using ecommerce.Domain.Interfaces;
using MediatR;

namespace ecommerce.Application.Products.Queries.GetAllProducts;

public class GetAllProductsQueryHandler(IProductRepository productRepository) 
    : IRequestHandler<GetAllProductsQuery, List<ProductDto>>
{
    public async Task<List<ProductDto>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await productRepository.GetAllProducts(cancellationToken);

        return products.Select(p => new ProductDto(
            p.Id, p.Name, p.Price, p.Code, p.AvailableQuantity, p.CategoryId)).ToList();
    }
}
