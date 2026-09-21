using MediatR;

namespace ecommerce.Application.Categories.Commands.UpdateCategory;

public record UpdateCategoryDto
{
    public string Name { get; set; }
}

public record UpdateCategoryCommand(Guid Id, UpdateCategoryDto dto) : IRequest<Guid>;