using FluentValidation;

namespace ecommerce.Application.Products.Commands.UpdateProduct;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProduct>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();

        RuleFor(x => x.ProductDto.Name)
            .NotEmpty().MaximumLength(200);

        RuleFor(x => x.ProductDto.Code)
            .NotEmpty().MaximumLength(50);

        RuleFor(x => x.ProductDto.Price)
            .GreaterThan(0);

        RuleFor(x => x.ProductDto.AvailableQuantity)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.ProductDto.CategoryId)
            .NotEmpty();
    }
}