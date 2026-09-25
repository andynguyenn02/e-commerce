using ecommerce.Domain.Enums;

namespace ecommerce.Application.Common.Models;

public record EmailRequest
{
    public EmailTypeEnum EmailType { get; set; }
    public Guid RelatedId { get; set; }
    public string RecipientEmail { get; set; }
}