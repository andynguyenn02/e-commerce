using ecommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Categories.Query.GetCategoryById;

public class GetCategoryByIdQueryHandler(IAppDbContext context)
    : IRequestHandler<GetCategoryByIdQuery, GetCategoryByIdDto>
{
    public async Task<GetCategoryByIdDto> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var exist = await context.Categories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        return exist is null
            ? throw new KeyNotFoundException("Category not found")
            : new GetCategoryByIdDto(exist.Id, exist.Name);
    }
}