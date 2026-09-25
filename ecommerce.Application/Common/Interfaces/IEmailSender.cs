namespace ecommerce.Application.Common.Interfaces;

public interface IEmailSender
{
    public Task SendEmailAsync(
        string email,
        string title,
        string content,
        CancellationToken cancellationToken);
}