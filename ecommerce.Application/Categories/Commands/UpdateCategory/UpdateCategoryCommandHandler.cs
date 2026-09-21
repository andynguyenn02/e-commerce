using ecommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Categories.Commands.UpdateCategory;

public class UpdateCategoryCommandHandler(IAppDbContext context) : IRequestHandler<UpdateCategoryCommand, Guid>
{
    public async Task<Guid> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var exist = await context.Categories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (exist is null) throw new KeyNotFoundException("Category not exist");

        exist.Name = request.dto.Name;

        await context.SaveChangesAsync(cancellationToken);
        return request.Id;
    }
}