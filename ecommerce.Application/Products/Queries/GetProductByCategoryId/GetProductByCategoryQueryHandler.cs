using ecommerce.Domain.Interfaces;
using MediatR;

namespace ecommerce.Application.Products.Queries.GetProductByCategoryId;

public class GetProductByCategoryQueryHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository) : IRequestHandler<GetProductByCategoryQuery, List<GetProductByCategoryDto>>
{
    public async Task<List<GetProductByCategoryDto>> Handle(GetProductByCategoryQuery request, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetCategoryById(request.CategoryId, cancellationToken)
                       ?? throw new KeyNotFoundException("Category not found");

        var products = await productRepository.GetProductsByCategory(category, cancellationToken)
            ?? throw new KeyNotFoundException("Product for this category not found");

        return products.Select((p) =>
            new GetProductByCategoryDto(p.Id, p.Name, p.Price, p.Code, p.AvailableQuantity, p.CategoryId)).ToList();
    }
}