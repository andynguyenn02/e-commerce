using FluentValidation;

namespace ecommerce.Application.Products.Commands.UpdatePrice;

public class UpdatePriceValidator : AbstractValidator<UpdatePriceCommand>
{
    public UpdatePriceValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().NotNull();
        RuleFor(x => x.Dto.Price).GreaterThan(0);
    }
}