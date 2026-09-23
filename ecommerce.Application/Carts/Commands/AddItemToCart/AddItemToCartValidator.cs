using FluentValidation;

namespace ecommerce.Application.Carts.Commands.AddItemToCart;

public class AddItemToCartValidator : AbstractValidator<AddItemToCartCommand>
{
    public AddItemToCartValidator()
    {
        RuleFor(x => x.ProductId).NotNull().NotEmpty();
        RuleFor(x => x.Quantity).NotNull().NotEmpty().GreaterThan(0);
    }
}