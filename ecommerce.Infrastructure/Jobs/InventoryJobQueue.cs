using System.Threading.Channels;
using ecommerce.Application.Common.Interfaces;

namespace ecommerce.Infrastructure.Jobs;

public class InventoryJobQueue : IInventoryJobQueue
{
    private readonly Channel<Guid> _chanel = Channel.CreateUnbounded<Guid>();

    public async Task PushToQueue(Guid jobId)
    {
        await _chanel.Writer.WriteAsync(jobId);
    }

    public IAsyncEnumerable<Guid> ReadQueue(CancellationToken ct = default)
    {
        return _chanel.Reader.ReadAllAsync(ct);
    }
}