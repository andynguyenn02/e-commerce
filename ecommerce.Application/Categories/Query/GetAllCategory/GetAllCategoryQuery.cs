using MediatR;

namespace ecommerce.Application.Categories.Query;

public class GetAllCategoryQuery : IRequest<List<GetAllCategoryDto>>
{
}