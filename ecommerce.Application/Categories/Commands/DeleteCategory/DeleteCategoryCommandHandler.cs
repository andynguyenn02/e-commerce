using ecommerce.Application.Common.Exceptions;
using ecommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Categories.Commands.DeleteCategory;

public class DeleteCategoryCommandHandler(IAppDbContext context) : IRequestHandler<DeleteCategoryCommand, Guid>
{
    public async Task<Guid> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var exist = await context.Categories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (exist is null) throw new NotFoundException("Category");

        context.Categories.Remove(exist);
        await context.SaveChangesAsync(cancellationToken);
        return exist.Id;
    }
}