using FluentValidation;

namespace ecommerce.Application.Carts.Commands.RemoveCartItem;

public class RemoveCartItemValidator : AbstractValidator<RemoveCartItemCommand>
{
    public RemoveCartItemValidator()
    {
        RuleFor(x => x.CartItemId).NotEmpty().NotNull();
    }
}