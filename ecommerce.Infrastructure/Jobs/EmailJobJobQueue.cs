using System.Threading.Channels;
using ecommerce.Application.Common.Interfaces;
using ecommerce.Domain.Enums;

namespace ecommerce.Infrastructure.Jobs;

public class EmailJobJobQueue : IEmailJobQueue
{
    private readonly Channel<EmailJobRequest> _channel = Channel.CreateUnbounded<EmailJobRequest>();

    public async Task PushToQueue(EmailTypeEnum emailType, Guid relatedId, string recipientEmail,
        CancellationToken ct = default)
    {
        await _channel.Writer.WriteAsync(new EmailJobRequest(emailType, relatedId, recipientEmail), ct);
    }

    public IAsyncEnumerable<EmailJobRequest> ReadQueue(CancellationToken ct = default)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }
}