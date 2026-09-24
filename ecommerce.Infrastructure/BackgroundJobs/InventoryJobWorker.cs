using System.Globalization;
using CsvHelper;
using ecommerce.Application.Common.Interfaces;
using ecommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ecommerce.Infrastructure.BackgroundJobs;

public class InventoryJobWorker(
    IInventoryJobQueue jobQueue,
    IServiceScopeFactory scopeFactory,
    IFileStorage fileStorage) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var jobId in jobQueue.ReadQueue(stoppingToken))
        {
            using var scope = scopeFactory.CreateScope();

            var service = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var job = await service.InventoryJobs
                .FirstOrDefaultAsync(j => j.Id == jobId, stoppingToken);

            if (job == null) throw new Exception($"Job with id {jobId} does not exist");

            await using var transaction = await service.BeginTransactionAsync(stoppingToken);

            job.Status = InventoryJobStatusEnum.Processing;
            await service.SaveChangesAsync(stoppingToken);

            try
            {
                await using var file = fileStorage.OpenForRead(job.StoredFileName);

                using var reader = new StreamReader(file);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                await csv.ReadAsync();
                csv.ReadHeader();

                var headers = csv.HeaderRecord;

                var expectedHeaders = new[]
                {
                    "Code",
                    "Quantity",
                    "Price"
                };

                if (headers == null || !expectedHeaders.ToHashSet().SetEquals(headers))
                    throw new Exception(
                        $"Invalid header format. Expected: [{string.Join(", ", expectedHeaders)}], " +
                        $"Actual: [{string.Join(", ", headers ?? [])}]");

                while (await csv.ReadAsync())
                {
                    var productCode = csv.GetField("Code");
                    var quantity = csv.GetField("Quantity");
                    var productPrice = csv.GetField("Price");

                    var product = await service.Products.FirstOrDefaultAsync(p => p.Code == productCode, stoppingToken);

                    if (!int.TryParse(quantity, out var quantityValue))
                        throw new Exception($"Invalid quantity: {quantity}");

                    if (product == null) throw new Exception($"Product with code {productCode} does not exist");

                    if (!decimal.TryParse(productPrice, out var productPriceValue))
                        throw new Exception($"Invalid product price: {productPrice} with code {productCode}");

                    product.AvailableQuantity = quantityValue;
                    product.Price = productPriceValue;
                }

                job.Status = InventoryJobStatusEnum.Accepted;
                fileStorage.MoveToArchive(job.StoredFileName);
                await service.SaveChangesAsync(stoppingToken);
                await transaction.CommitAsync(stoppingToken);
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync(stoppingToken);
                job.Status = InventoryJobStatusEnum.Failed;
                job.ErrorMessage = e.Message;
                fileStorage.MoveToFailed(job.StoredFileName);
                await service.SaveChangesAsync(stoppingToken);
            }
        }
    }
}