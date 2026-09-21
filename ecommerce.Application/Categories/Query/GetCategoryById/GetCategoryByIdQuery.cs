using MediatR;

namespace ecommerce.Application.Categories.Query.GetCategoryById;

public record GetCategoryByIdQuery(Guid Id) : IRequest<GetCategoryByIdDto>
{
}