using ecommerce.Domain.Enums;

namespace ecommerce.Application.Inventory.Queries;

public record JobListDto(
    Guid JobId,
    string FileName,
    string Status,
    DateTime CreatedAt,
    DateTime? EmailSentAt,
    string? ErrorMessage);