using ecommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Categories.Query;

public class GetAllCategoryQueryHandler(IAppDbContext context)
    : IRequestHandler<GetAllCategoryQuery, List<GetAllCategoryDto>>
{
    public async Task<List<GetAllCategoryDto>> Handle(GetAllCategoryQuery request, CancellationToken cancellationToken)
    {
        var list = await context.Categories.Select(p => new GetAllCategoryDto(p.Id, p.Name))
            .ToListAsync(cancellationToken);

        return list;
    }
}