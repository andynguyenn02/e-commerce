using System.Globalization;
using ecommerce.Application.Common.Interfaces;
using ecommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ecommerce.Infrastructure.BackgroundJobs;

public class InventoryJobWorker(
    IInventoryJobQueue jobQueue,
    IServiceScopeFactory scopeFactory,
    IFileStorage fileStorage,
    IEnumerable<IInventoryFileReader> fileReaders,
    IEmailJobQueue emailJobQueue,
    ILogger<InventoryJobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var jobId in jobQueue.ReadQueue(stoppingToken))
        {
            using var scope = scopeFactory.CreateScope();

            var service = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var job = await service.InventoryJobs
                .FirstOrDefaultAsync(j => j.Id == jobId, stoppingToken);

            if (job == null)
            {
                logger.LogWarning("Inventory job {JobId} was queued but not found in the database", jobId);
                continue;
            }

            job.Status = InventoryJobStatusEnum.Processing;
            await service.SaveChangesAsync(stoppingToken);

            try
            {
                var fileReader = fileReaders.FirstOrDefault(f => f.CanRead(job.StoredFileName)) ??
                                 throw new Exception($"Unsupported file type: {Path.GetExtension(job.StoredFileName)}");

                await using var file = fileStorage.OpenForRead(job.StoredFileName);

                foreach (var row in fileReader.Read(file))
                {
                    if (!int.TryParse(row.Quantity, NumberStyles.Integer, CultureInfo.InvariantCulture,
                            out var quantityValue))
                        throw new Exception($"Row {row.RowNumber}: invalid quantity '{row.Quantity}'");

                    if (!decimal.TryParse(row.Price, NumberStyles.Number, CultureInfo.InvariantCulture,
                            out var priceValue))
                        throw new Exception($"Row {row.RowNumber}: invalid price '{row.Price}' for code {row.Code}");

                    var product = await service.Products
                                      .FirstOrDefaultAsync(p => p.Code == row.Code, stoppingToken)
                                  ?? throw new Exception(
                                      $"Row {row.RowNumber}: product with code {row.Code} does not exist");

                    product.AvailableQuantity = quantityValue;
                    product.Price = priceValue;
                }

                job.Status = InventoryJobStatusEnum.Accepted;
                fileStorage.MoveToArchive(job.StoredFileName);
                await service.SaveChangesAsync(stoppingToken);

                // Enqueue upload notification email after successful processing
                var adminUser = await service.Users.FirstAsync(u => u.Id == job.UserId, stoppingToken);
                await emailJobQueue.PushToQueue(EmailTypeEnum.Upload, job.Id, adminUser.UserName, stoppingToken);
            }
            catch (Exception e)
            {
                job.Status = InventoryJobStatusEnum.Failed;
                job.ErrorMessage = e.Message;

                fileStorage.MoveToFailed(job.StoredFileName);
                await service.SaveChangesAsync(stoppingToken);

                logger.LogError(e, "Inventory job {JobId} failed", jobId);
            }
        }
    }
}