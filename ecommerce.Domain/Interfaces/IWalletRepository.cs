using ecommerce.Domain.Entities;

namespace ecommerce.Domain.Interfaces;

public interface IWalletRepository
{
    public Task<List<WalletEntity>> GetAllWallets();
    public Task<WalletEntity?> GetWalletById(Guid id);
    public Task<WalletEntity> CreateWallet(WalletEntity wallet);
    public Task UpdateWallet(WalletEntity wallet);
    public Task DeleteWallet(WalletEntity wallet);
    public Task<WalletEntity?> GetWalletByUserId(Guid userId);
    
    public Task<WalletTransactionEntity> CreateTransaction(Guid walletId, WalletTransactionEntity walletTransaction);
    public Task<List<WalletTransactionEntity>> GetTransactionsByWalletId(Guid walletId);
}