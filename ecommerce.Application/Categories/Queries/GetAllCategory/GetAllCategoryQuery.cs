using MediatR;

namespace ecommerce.Application.Categories.Queries;

public class GetAllCategoryQuery : IRequest<List<GetAllCategoryDto>>
{
}