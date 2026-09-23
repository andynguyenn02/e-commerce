using MediatR;

namespace ecommerce.Application.Categories.Queries.GetCategoryById;

public record GetCategoryByIdQuery(Guid Id) : IRequest<GetCategoryByIdDto>
{
}