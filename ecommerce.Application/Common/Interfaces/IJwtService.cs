namespace ecommerce.Application.Common.Interfaces;

public interface IJwtService
{
    public string GenerateToken(Guid userId, string username, string role);
}