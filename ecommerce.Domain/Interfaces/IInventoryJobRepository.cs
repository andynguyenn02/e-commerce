using ecommerce.Domain.Entities;

namespace ecommerce.Domain.Interfaces;

public interface IInventoryJobRepository
{
    public Task<List<InventoryJobEntity>> GetAllInventoryJobs();
    public Task<InventoryJobEntity?> GetInventoryJobById(Guid id);
    public Task<InventoryJobEntity> CreateInventoryJob(InventoryJobEntity job);
    public Task UpdateInventoryJob(InventoryJobEntity job);
    public Task DeleteInventoryJob(InventoryJobEntity job);
}