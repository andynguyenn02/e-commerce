using System.Security.Claims;
using ecommerce.Application.Authentication.Commands.Login;
using ecommerce.Application.Authentication.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ecommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand request, CancellationToken ct)
    {
        var token = await sender.Send(request, ct);

        Response.Cookies.Append("access_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(15)
        });

        return Ok();
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand request, CancellationToken ct)
    {
        await sender.Send(request, ct);
        return Created();
    }

    [HttpPost("logout")]
    public IActionResult Logout(CancellationToken ct)
    {
        Response.Cookies.Delete("access_token");
        return NoContent();
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            id = User.FindFirstValue("sub"),
            username = User.FindFirstValue("unique_name"),
            role = User.FindFirstValue("role")
        });
    }
}