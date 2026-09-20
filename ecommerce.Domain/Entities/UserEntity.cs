using ecommerce.Domain.Enums;

namespace ecommerce.Domain.Entities;

public class UserEntity : CommonEntity
{
    public required string UserName { get; set; }
    public required string PasswordHash { get; set; }
    public required RoleEnum Role { get; set; }
}