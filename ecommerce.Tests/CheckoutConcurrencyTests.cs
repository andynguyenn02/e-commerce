using ecommerce.Application.Checkout.Commands.CheckoutCommand;
using ecommerce.Application.Common.Exceptions;
using ecommerce.Application.Common.Interfaces;
using ecommerce.Domain.Entities;
using ecommerce.Domain.Enums;
using ecommerce.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ecommerce.Tests;

file sealed class TestCurrentUser : ICurrentUser
{
    public required Guid UserId { get; set; }
    public required string UserName { get; set; }
    public RoleEnum Role => RoleEnum.Customer;
    public bool IsAuthenticated => true;
}

file sealed class NoopEmailQueue : IEmailJobQueue
{
    public Task PushToQueue(EmailTypeEnum emailType, Guid relatedId, string recipientEmail, CancellationToken ct = default)
        => Task.CompletedTask;

    public IAsyncEnumerable<EmailJobRequest> ReadQueue(CancellationToken ct = default)
        => throw new NotSupportedException();
}

public sealed class CheckoutDatabaseFixture : IAsyncLifetime
{
    private static string BuildTestConnectionString()
    {
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddUserSecrets("f2062c04-cc70-44dd-a495-164dbb08c58b")
            .Build();

        var connectionString = config["ConnectionStrings:DefaultConnection"];

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not set. Set the ConnectionStrings__DefaultConnection " +
                "environment variable, or run `dotnet user-secrets set ConnectionStrings:DefaultConnection \"...\"` " +
                "on ecommerce.Api (this test project shares its UserSecretsId).");

        // Never run tests against the dev database: force a dedicated test catalog.
        var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "EcommerceDb_Tests" };
        return builder.ConnectionString;
    }

    public string ConnectionString { get; } = BuildTestConnectionString();

    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(ConnectionString).Options;
        return new ApplicationDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

public class CheckoutConcurrencyTests(CheckoutDatabaseFixture fixture) : IClassFixture<CheckoutDatabaseFixture>
{
    [Fact]
    public async Task Concurrent_checkout_does_not_oversell()
    {
        var suffix = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var userIds = new List<Guid>();
        var cartItemIdByUser = new Dictionary<Guid, Guid>();

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

            for (var i = 0; i < 20; i++)
            {
                var userId = Guid.NewGuid();
                var cartId = Guid.NewGuid();
                var cartItemId = Guid.NewGuid();

                var wallet = new WalletEntity { Id = Guid.NewGuid(), UserId = userId };

                seed.Users.Add(new UserEntity
                {
                    Id = userId,
                    UserName = $"user-{suffix}-{i}",
                    PasswordHash = "x",
                    Role = RoleEnum.Customer
                });
                seed.Wallets.Add(wallet);
                seed.Carts.Add(new CartEntity { Id = cartId, UserId = userId });
                seed.CartItems.Add(new CartItemEntity { Id = cartItemId, CartId = cartId, ProductId = productId, Quantity = 1 });

                userIds.Add(userId);
                cartItemIdByUser[userId] = cartItemId;
            }

            await seed.SaveChangesAsync(CancellationToken.None);
        }

        var results = await Task.WhenAll(userIds.Select(async userId =>
        {
            await using var context = fixture.CreateContext();
            var currentUser = new TestCurrentUser { UserId = userId, UserName = $"user-{userId}" };
            var handler = new CheckoutCommandHandler(context, currentUser, new NoopEmailQueue());

            try
            {
                await handler.Handle(new CheckoutCommand([cartItemIdByUser[userId]]), CancellationToken.None);
                return (Success: true, Exception: (Exception?)null);
            }
            catch (Exception ex)
            {
                return (Success: false, Exception: ex);
            }
        }));

        var succeeded = results.Count(r => r.Success);
        var failed = results.Where(r => !r.Success).ToList();

        Assert.Equal(5, succeeded);
        Assert.Equal(15, failed.Count);
        foreach (var failure in failed)
            Assert.True(failure.Exception is InsufficientStockException,
                $"Expected InsufficientStockException but got: {failure.Exception}");

        await using var verify = fixture.CreateContext();

        var finalQuantity = await verify.Products
            .Where(p => p.Id == productId)
            .Select(p => p.AvailableQuantity)
            .FirstAsync();
        Assert.Equal(0, finalQuantity);

        var orderIds = await verify.Orders
            .Where(o => userIds.Contains(o.UserId))
            .Select(o => o.Id)
            .ToListAsync();
        Assert.Equal(5, orderIds.Count);

        var orderItems = await verify.OrderItems
            .Where(oi => orderIds.Contains(oi.OrderId))
            .ToListAsync();
        Assert.Equal(5, orderItems.Count);
        Assert.All(orderItems, oi => Assert.Equal(1, oi.Quantity));
        Assert.All(orderItems, oi => Assert.Equal(10, oi.PriceAtPurchased));

        var walletIds = await verify.Wallets
            .Where(w => userIds.Contains(w.UserId))
            .Select(w => w.Id)
            .ToListAsync();
        var walletTransactionCount = await verify.WalletTransactions
            .CountAsync(wt => walletIds.Contains(wt.WalletId));
        Assert.Equal(5, walletTransactionCount);
    }

    [Fact]
    public async Task Concurrent_checkout_does_not_double_spend_wallet()
    {
        var suffix = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var productAId = Guid.NewGuid();
        var productBId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var cartItemAId = Guid.NewGuid();
        var cartItemBId = Guid.NewGuid();

        await using (var seed = fixture.CreateContext())
        {
            seed.Categories.Add(new CategoryEntity { Id = categoryId, Name = $"Category-{suffix}" });
            seed.Products.Add(new ProductEntity
            {
                Id = productAId, Name = $"Product-A-{suffix}", Price = 1000, Code = $"CODE-A-{suffix}",
                AvailableQuantity = 10, CategoryId = categoryId, IsDeleted = false
            });
            seed.Products.Add(new ProductEntity
            {
                Id = productBId, Name = $"Product-B-{suffix}", Price = 1000, Code = $"CODE-B-{suffix}",
                AvailableQuantity = 10, CategoryId = categoryId, IsDeleted = false
            });

            // default wallet balance of 1000, enough for one 1000-priced item only
            var wallet = new WalletEntity { Id = Guid.NewGuid(), UserId = userId };

            var cartId = Guid.NewGuid();
            seed.Users.Add(new UserEntity
            {
                Id = userId, UserName = $"user-{suffix}", PasswordHash = "x", Role = RoleEnum.Customer
            });
            seed.Wallets.Add(wallet);
            seed.Carts.Add(new CartEntity { Id = cartId, UserId = userId });
            seed.CartItems.Add(new CartItemEntity { Id = cartItemAId, CartId = cartId, ProductId = productAId, Quantity = 1 });
            seed.CartItems.Add(new CartItemEntity { Id = cartItemBId, CartId = cartId, ProductId = productBId, Quantity = 1 });

            await seed.SaveChangesAsync(CancellationToken.None);
        }

        var results = await Task.WhenAll(new[] { cartItemAId, cartItemBId }.Select(async cartItemId =>
        {
            await using var context = fixture.CreateContext();
            var currentUser = new TestCurrentUser { UserId = userId, UserName = $"user-{suffix}" };
            var handler = new CheckoutCommandHandler(context, currentUser, new NoopEmailQueue());

            try
            {
                await handler.Handle(new CheckoutCommand([cartItemId]), CancellationToken.None);
                return (Success: true, Exception: (Exception?)null);
            }
            catch (Exception ex)
            {
                return (Success: false, Exception: ex);
            }
        }));

        var succeeded = results.Count(r => r.Success);
        var failed = results.Where(r => !r.Success).ToList();

        Assert.Equal(1, succeeded);
        Assert.Single(failed);
        Assert.True(failed[0].Exception is InsufficientBalanceException,
            $"Expected InsufficientBalanceException but got: {failed[0].Exception}");

        await using var verify = fixture.CreateContext();

        var walletBalance = await verify.Wallets
            .Where(w => w.UserId == userId)
            .Select(w => w.Balance)
            .FirstAsync();
        Assert.Equal(0m, walletBalance);

        var orderCount = await verify.Orders.CountAsync(o => o.UserId == userId);
        Assert.Equal(1, orderCount);

        var walletId = await verify.Wallets.Where(w => w.UserId == userId).Select(w => w.Id).FirstAsync();
        var walletTransactionCount = await verify.WalletTransactions.CountAsync(wt => wt.WalletId == walletId);
        Assert.Equal(1, walletTransactionCount);
    }
}
