using ecommerce.Application.Common.Interfaces;
using ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<ProductEntity> Products { get; }
    public DbSet<CategoryEntity> Categories { get; }
    public DbSet<CartEntity> Carts { get; }
    public DbSet<OrderEntity> Orders { get; }
    public DbSet<InventoryJobEntity> InventoryJobs { get; }
    public DbSet<CartItemEntity> CartItems { get; }
    public DbSet<OrderItemEntity> OrderItems { get; }
    public DbSet<UserEntity> Users { get; }
    public DbSet<WalletEntity> Wallets { get; }
    public DbSet<WalletTransactionEntity> WalletTransactions { get; }
}