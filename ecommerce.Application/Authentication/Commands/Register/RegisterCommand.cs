using MediatR;

namespace ecommerce.Application.Authentication.Commands.Register;

public record RegisterCommand : IRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}