using ecommerce.Domain.Enums;

namespace ecommerce.Domain.Entities;

public class InventoryJobEntity: CommonEntity
{
    public required Guid UserId { get; set; }
    public required UserEntity User { get; set; }
    
    public required string FileName { get; set; }
    public required InventoryJobStatusEnum Status { get; set; }
    public DateTime EmailSentAt { get; set; }
    public string? ErrorMessage { get; set; }
}