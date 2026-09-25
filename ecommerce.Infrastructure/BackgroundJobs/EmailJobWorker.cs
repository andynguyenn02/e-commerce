using ecommerce.Application.Common.Interfaces;
using ecommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ecommerce.Infrastructure.BackgroundJobs;

public class EmailJobWorker(
    IEmailJobQueue jobQueue,
    IServiceScopeFactory scopeFactory,
    IEmailSender emailSender,
    ILogger<EmailJobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var request in jobQueue.ReadQueue(stoppingToken))
            try
            {
                using var scope = scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

                switch (request.EmailType)
                {
                    case EmailTypeEnum.Checkout:
                        await ProcessCheckoutEmailAsync(request.RelatedId, request.RecipientEmail, context,
                            stoppingToken);
                        break;

                    case EmailTypeEnum.Upload:
                        await ProcessUploadEmailAsync(request.RelatedId, request.RecipientEmail, context,
                            stoppingToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process email job for type {EmailType}, relatedId {RelatedId}",
                    request.EmailType, request.RelatedId);
            }
    }

    private async Task ProcessCheckoutEmailAsync(Guid orderId, string recipientEmail, IAppDbContext context,
        CancellationToken ct)
    {
        var order = await context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null)
        {
            logger.LogWarning("Order {OrderId} not found for checkout email", orderId);
            return;
        }

        var orderItems = await context.OrderItems
            .Include(oi => oi.Product)
            .Where(oi => oi.OrderId == orderId)
            .ToListAsync(ct);

        var subject = $"Order Confirmation - #{order.Id}";
        var body = $"""
                    <h2>Order Confirmation</h2>
                    <p>Your order <strong>#{order.Id}</strong> has been placed successfully.</p>
                    <h3>Items</h3>
                    <table border="1" cellpadding="5" cellspacing="0">
                        <tr><th>Product</th><th>Qty</th><th>Price</th></tr>
                        {string.Join("", orderItems.Select(oi => $"<tr><td>{oi.Product?.Name ?? "Unknown"}</td><td>{oi.Quantity}</td><td>{oi.PriceAtPurchased:N0}</td></tr>"))}
                    </table>
                    <p><strong>Total:</strong> {orderItems.Sum(oi => oi.Quantity * oi.PriceAtPurchased):N0}</p>
                    """;

        try
        {
            await emailSender.SendEmailAsync("noreply@dev.local", subject, body, ct);
            order.EmailSentAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);
            logger.LogInformation("Checkout email sent for order {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send checkout email for order {OrderId}", orderId);
            order.EmailSentAt = null;
            await context.SaveChangesAsync(ct);
        }
    }

    private async Task ProcessUploadEmailAsync(Guid jobId, string recipientEmail, IAppDbContext context,
        CancellationToken ct)
    {
        var inventoryJob = await context.InventoryJobs
            .Include(j => j.User)
            .FirstOrDefaultAsync(j => j.Id == jobId, ct);

        if (inventoryJob == null)
        {
            logger.LogWarning("Inventory job {JobId} not found for upload email", jobId);
            return;
        }

        var subject = $"Inventory Upload Completed - {inventoryJob.OriginalFileName}";
        var body = $"""
                    <h2>Inventory Upload Result</h2>
                    <p>File <strong>{inventoryJob.OriginalFileName}</strong> has been processed.</p>
                    <p><strong>Status:</strong> {inventoryJob.Status}</p>
                    <p><strong>Processed at:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                    """;

        try
        {
            await emailSender.SendEmailAsync("noreply@dev.local", subject, body, ct);
            inventoryJob.EmailSentAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);
            logger.LogInformation("Upload email sent for inventory job {JobId}", jobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send upload email for inventory job {JobId}", jobId);
            inventoryJob.EmailSentAt = null;
            await context.SaveChangesAsync(ct);
        }
    }
}