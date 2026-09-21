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

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTimeStamp();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyTimeStamp();
        return base.SaveChanges();
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProductEntity>().HasIndex(p => p.Code).IsUnique().HasFilter("[IsDeleted] = 0");
        modelBuilder.Entity<ProductEntity>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<UserEntity>().HasIndex(u => u.UserName).IsUnique();
        modelBuilder.Entity<CartEntity>().HasIndex(c => c.UserId).IsUnique();
        modelBuilder.Entity<WalletTransactionEntity>()
            .HasOne(wt => wt.Order)
            .WithMany()
            .HasForeignKey(wt => wt.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    //Automate assign created and updated timestamp
    private void ApplyTimeStamp()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<CommonEntity>())
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.Id = Guid.CreateVersion7();
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
    }
}