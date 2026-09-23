using System.Security.Claims;
using ecommerce.Application.Common.Interfaces;
using ecommerce.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace ecommerce.Infrastructure.Security;

public class CurrentUser : ICurrentUser
{
    private readonly ClaimsPrincipal? _user;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _user = httpContextAccessor.HttpContext?.User;
    }

    public bool IsAuthenticated => _user?.Identity?.IsAuthenticated ?? false;

    public Guid UserId
    {
        get
        {
            var value = _user?.FindFirst("sub")?.Value;
            return Guid.TryParse(value, out var result) ? result : Guid.Empty;
        }
    }

    public string? UserName
    {
        get
        {
            var value = _user?.FindFirst("unique_name")?.Value;
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }

    public RoleEnum Role
    {
        get
        {
            var value = _user?.FindFirst("role")?.Value;
            Enum.TryParse<RoleEnum>(value, out var result);
            return result;
        }
    }
}