using ecommerce.Domain.Enums;

namespace ecommerce.Domain.Entities;

public class InventoryJobEntity: CommonEntity
{
    public required Guid UserId { get; set; }
    public UserEntity? User { get; set; }
    
    public required string OriginalFileName { get; set; }
    public required string StoredFileName { get; set; }
    public required InventoryJobStatusEnum Status { get; set; }
    public DateTime? EmailSentAt { get; set; }
    public string? ErrorMessage { get; set; }
}