using ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<ProductEntity> Products { get; set; }
    DbSet<CategoryEntity> Categories { get; set; }
    DbSet<CartEntity> Carts { get; set; }
    DbSet<OrderEntity> Orders { get; set; }
    DbSet<InventoryJobEntity> InventoryJobs { get; set; }
    DbSet<CartItemEntity> CartItems { get; set; }
    DbSet<OrderItemEntity> OrderItems { get; set; }
    DbSet<UserEntity> Users { get; set; }
    DbSet<WalletEntity> Wallets { get; set; }
    DbSet<WalletTransactionEntity> WalletTransactions { get; set; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}