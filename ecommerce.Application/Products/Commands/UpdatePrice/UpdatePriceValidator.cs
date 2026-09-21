using FluentValidation;

namespace ecommerce.Application.Products.Commands.UpdatePrice;

public class UpdatePriceValidator : AbstractValidator<UpdatePriceCommand>
{
    public UpdatePriceValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().NotEmpty();
        RuleFor(x => x.Dto.Price).NotEmpty().NotEmpty();
    }
}