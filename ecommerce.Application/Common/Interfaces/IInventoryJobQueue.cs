namespace ecommerce.Application.Common.Interfaces;

public interface IInventoryJobQueue
{
    public Task PushToQueue(Guid jobId);
    public IAsyncEnumerable<Guid> ReadQueue(CancellationToken ct = default);
}