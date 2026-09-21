namespace ecommerce.Infrastructure.Security;

public class JwtSettings
{
    public const string Section = "JwtSettings";

    public string Key { get; set; } = "YourSecretKeyHereYourSecretKeyHereYourSecretKeyHere";
    public string Issuer { get; set; } = "yourdomain.com";
    public string Audience { get; set; } = "yourdomain.com";
    public string CookieName { get; set; } = "access_token";

    public int ExpiryMinutes { get; set; } = 180;
}