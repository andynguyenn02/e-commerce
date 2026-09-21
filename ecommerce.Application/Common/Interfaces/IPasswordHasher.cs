namespace ecommerce.Application.Common.Interfaces;

public interface IPasswordHasher
{
    public string GenerateHash(string password);
    public bool VerifyHash(string hash, string password);
}