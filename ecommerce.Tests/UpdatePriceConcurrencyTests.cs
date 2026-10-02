using ecommerce.Application.Common.Exceptions;
using ecommerce.Application.Products.Commands.UpdatePrice;
using ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Tests;

public class UpdatePriceConcurrencyTests(CheckoutDatabaseFixture fixture) : IClassFixture<CheckoutDatabaseFixture>
{
    [Fact]
    public async Task Replaying_stale_expected_price_throws_conflict()
    {
        var suffix = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        await using (var seed = fixture.CreateContext())
        {
            seed.Categories.Add(new CategoryEntity { Id = categoryId, Name = $"Category-{suffix}" });
            seed.Products.Add(new ProductEntity
            {
                Id = productId,
                Name = $"Product-{suffix}",
                Price = 10,
                Code = $"CODE-{suffix}",
                AvailableQuantity = 5,
                CategoryId = categoryId,
                IsDeleted = false
            });

            await seed.SaveChangesAsync(CancellationToken.None);
        }

        await using var context = fixture.CreateContext();
        var handler = new UpdatePriceCommandHandler(context);

        await handler.Handle(
            new UpdatePriceCommand(productId, new UpdatePriceDto(Price: 20, ExpectedPrice: 10)),
            CancellationToken.None);

        Exception? caught = null;
        try
        {
            await handler.Handle(
                new UpdatePriceCommand(productId, new UpdatePriceDto(Price: 30, ExpectedPrice: 10)),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            caught = ex;
        }

        Assert.True(caught is ConflictException, $"Expected ConflictException but got: {caught}");

        await using var verify = fixture.CreateContext();
        var finalPrice = await verify.Products
            .Where(p => p.Id == productId)
            .Select(p => p.Price)
            .FirstAsync();
        Assert.Equal(20, finalPrice);
    }
}
