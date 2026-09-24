using ecommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Inventory.Queries.GetJobList;

public class GetJobListHandler(IAppDbContext context)
    : IRequestHandler<GetJobListQuery, List<JobListDto>>
{
    public async Task<List<JobListDto>> Handle(GetJobListQuery request, CancellationToken cancellationToken)
    {
        var list = await context.InventoryJobs
            .OrderByDescending(j => j.CreatedAt)
            .Select(i => new JobListDto(i.Id, i.OriginalFileName, i.Status.ToString(), i.CreatedAt, i.EmailSentAt,
                i.ErrorMessage))
            .ToListAsync(cancellationToken);

        return list;
    }
}