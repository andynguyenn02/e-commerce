using ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<ProductEntity> Products { get; }
    DbSet<CategoryEntity> Categories { get; }
    DbSet<CartEntity> Carts { get; }
    DbSet<OrderEntity> Orders { get; }
    DbSet<InventoryJobEntity> InventoryJobs { get; }
    DbSet<CartItemEntity> CartItems { get; }
    DbSet<OrderItemEntity> OrderItems { get; }
    DbSet<UserEntity> Users { get; }
    DbSet<WalletEntity> Wallets { get; }
    DbSet<WalletTransactionEntity> WalletTransactions { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}