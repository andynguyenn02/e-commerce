using ecommerce.Domain.Enums;

namespace ecommerce.Application.Common.Interfaces;

public interface ICurrentUser
{
    public Guid UserId { get; }
    public RoleEnum Role { get; }
}