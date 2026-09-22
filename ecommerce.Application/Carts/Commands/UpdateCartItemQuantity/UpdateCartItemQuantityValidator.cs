using FluentValidation;

namespace ecommerce.Application.Carts.Commands.UpdateCartItemQuantity;

public class UpdateCartItemQuantityValidator : AbstractValidator<UpdateCartItemQuantityCommand>
{
    public UpdateCartItemQuantityValidator()
    {
        RuleFor(x => x.CartItemId).NotEmpty().NotNull();
        RuleFor(x => x.Dto.Quantity).NotEmpty().NotEmpty().GreaterThanOrEqualTo(0);
    }
}