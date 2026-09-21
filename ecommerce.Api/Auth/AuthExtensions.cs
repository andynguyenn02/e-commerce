using ecommerce.Domain.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace ecommerce.Api.Auth;

public static class AuthExtensions
{
    public static IServiceCollection AddApiAuth(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.ConfigureOptions<ConfigureJwtBearerOptions>();
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser().Build())
            .AddPolicy("Admin", p => p.RequireRole(nameof(RoleEnum.Admin)))
            .AddPolicy("Customer", p => p.RequireRole(nameof(RoleEnum.Customer)));
        return services;
    }
}