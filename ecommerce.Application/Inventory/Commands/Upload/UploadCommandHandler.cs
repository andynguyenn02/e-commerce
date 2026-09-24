using ecommerce.Application.Common.Exceptions;
using ecommerce.Application.Common.Interfaces;
using ecommerce.Domain.Entities;
using ecommerce.Domain.Enums;
using MediatR;

namespace ecommerce.Application.Inventory.Commands.Upload;

public class UploadCommandHandler(
    IAppDbContext context,
    ICurrentUser currentUser,
    IFileStorage storage,
    IInventoryJobQueue jobQueue)
    : IRequestHandler<UploadCommand, Guid>
{
    public async Task<Guid> Handle(UploadCommand request, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(request.OriginalFileName);

        if (extension != ".csv" && extension != ".xls" && extension != ".xlsx")
            throw new FileExtensionNotValid(request.OriginalFileName);

        var storedFileName = storage.SaveToUploads(request.Stream, request.OriginalFileName);

        var job = new InventoryJobEntity
        {
            UserId = currentUser.UserId,
            OriginalFileName = request.OriginalFileName,
            Status = InventoryJobStatusEnum.Stored,
            StoredFileName = storedFileName
        };

        context.InventoryJobs.Add(job);
        await context.SaveChangesAsync(cancellationToken);

        await jobQueue.PushToQueue(job.Id);

        return job.Id;
    }
}