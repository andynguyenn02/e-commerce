using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using ecommerce.Application.Common.Interfaces;

namespace ecommerce.Infrastructure.Email;

public class SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private string? FromUser { get; } = config["Smtp:FromAddress"];
    private string? FromName { get; } = config["Smtp:FromName"];
    private string? Host { get; } = config["Smtp:Host"];
    private int Port { get; } = int.Parse(config["Smtp:Port"] ?? "587");
    private bool EnableSsl { get; } = bool.Parse(config["Smtp:EnableSsl"] ?? "false");
    private string? Username { get; } = config["Smtp:Username"];
    private string? Password { get; } = config["Smtp:Password"];

    public async Task SendEmailAsync(
        string email,
        string title,
        string content,
        CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(FromName ?? "E-Commerce", FromUser ?? "noreply@dev.local"));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = title;
        message.Body = new TextPart("html") { Text = content };

        try
        {
            using var client = new SmtpClient();

            var options = EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await client.ConnectAsync(Host!, Port, options, cancellationToken);

            if (!string.IsNullOrEmpty(Username))
                await client.AuthenticateAsync(Username!, Password!, cancellationToken);

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to send email to {Recipient} with subject '{Subject}'", email, title);
            throw;
        }
    }
}
