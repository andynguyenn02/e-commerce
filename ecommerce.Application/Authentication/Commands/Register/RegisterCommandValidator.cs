using FluentValidation;

namespace ecommerce.Application.Authentication.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(loginCommand => loginCommand.Username).NotEmpty().NotNull().MinimumLength(8).MaximumLength(20);
        RuleFor(loginCommand => loginCommand.Password).NotEmpty().NotNull().MinimumLength(8);
    }
}