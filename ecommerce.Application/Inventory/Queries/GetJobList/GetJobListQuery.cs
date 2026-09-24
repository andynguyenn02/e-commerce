using MediatR;

namespace ecommerce.Application.Inventory.Queries.GetJobList;

public record GetJobListQuery : IRequest<List<JobListDto>>;