using ecommerce.Application.Common.Exceptions;
using ecommerce.Application.Common.Interfaces;
using ecommerce.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Categories.Commands.CreateCategory;

public class CreateCategoryCommandHandler(IAppDbContext context) : IRequestHandler<CreateCategoryCommand, Guid>
{
    public async Task<Guid> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var exist = await context.Categories.FirstOrDefaultAsync(c => c.Name == request.Name, cancellationToken);

        if (exist is not null)
            throw new CategoryAlreadyExistsException(request.Name);

        var category = new CategoryEntity { Name = request.Name };

        context.Categories.Add(category);

        await context.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}