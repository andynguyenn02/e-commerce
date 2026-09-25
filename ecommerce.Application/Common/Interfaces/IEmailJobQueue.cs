using ecommerce.Domain.Enums;

namespace ecommerce.Application.Common.Interfaces;

public interface IEmailJobQueue
{
    Task PushToQueue(EmailTypeEnum emailType, Guid relatedId, string recipientEmail, CancellationToken ct = default);
    IAsyncEnumerable<EmailJobRequest> ReadQueue(CancellationToken ct = default);
}

public record EmailJobRequest(EmailTypeEnum EmailType, Guid RelatedId, string RecipientEmail);