using ecommerce.Domain.Enums;

namespace ecommerce.Application.Common.Interfaces;

public interface ICurrentUser
{
    public Guid UserId { get; }
    public string UserName { get; }
    public RoleEnum Role { get; }
    public bool IsAuthenticated { get; }
}