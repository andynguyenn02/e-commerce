using ecommerce.Application.Common.Interfaces;

namespace ecommerce.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    public string GenerateHash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyHash(string hash, string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}