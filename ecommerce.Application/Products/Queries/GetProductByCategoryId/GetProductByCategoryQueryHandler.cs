using ecommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Products.Queries.GetProductByCategoryId;

public class GetProductByCategoryQueryHandler(
    IAppDbContext appDbContext) : IRequestHandler<GetProductByCategoryQuery, List<GetProductByCategoryDto>>
{
    public async Task<List<GetProductByCategoryDto>> Handle(GetProductByCategoryQuery request,
        CancellationToken cancellationToken)
    {
        var category = await appDbContext.Categories.FindAsync([request.CategoryId], cancellationToken)
                       ?? throw new KeyNotFoundException("Category not found");

        var products = await appDbContext.Products.Where(p => p.CategoryId == request.CategoryId)
            .Select(p =>
                new GetProductByCategoryDto(p.Id, p.Name, p.Price, p.Code, p.AvailableQuantity, p.CategoryId,
                    p.CreatedAt))
            .ToListAsync(cancellationToken);

        return products;
    }
}