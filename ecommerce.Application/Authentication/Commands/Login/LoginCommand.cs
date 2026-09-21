using MediatR;

namespace ecommerce.Application.Authentication.Commands.Login;

public record LoginCommand : IRequest<string>
{
    public string Username { get; set; }
    public string Password { get; set; }
}