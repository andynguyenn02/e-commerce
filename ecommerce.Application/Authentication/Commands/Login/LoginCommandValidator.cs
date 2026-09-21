using FluentValidation;

namespace ecommerce.Application.Authentication.Commands.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(loginCommand => loginCommand.Username).NotEmpty().NotNull();
        RuleFor(loginCommand => loginCommand.Password).NotEmpty().NotNull();
    }
}