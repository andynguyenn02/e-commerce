using ecommerce.Application.Common.Interfaces;
using ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<ProductEntity> Products { get; set; }
    public DbSet<CategoryEntity> Categories { get; set; }
    public DbSet<CartEntity> Carts { get; set; }
    public DbSet<OrderEntity> Orders { get; set; }
    public DbSet<InventoryJobEntity> InventoryJobs { get; set; }
    public DbSet<CartItemEntity> CartItems { get; set; }
    public DbSet<OrderItemEntity> OrderItems { get; set; }
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<WalletEntity> Wallets { get; set; }
    public DbSet<WalletTransactionEntity> WalletTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProductEntity>().HasIndex(p => p.Code).IsUnique();
        modelBuilder.Entity<UserEntity>().HasIndex(u => u.UserName).IsUnique();
        modelBuilder.Entity<CartEntity>().HasIndex(c => c.UserId).IsUnique();
        modelBuilder.Entity<WalletTransactionEntity>()
        .HasOne(wt => wt.Order)
        .WithMany()
        .HasForeignKey(wt => wt.OrderId)
        .OnDelete(DeleteBehavior.Restrict);
    }
}