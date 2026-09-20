using FluentValidation;

namespace ecommerce.Application.Products.Commands.UpdateProduct;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        // RuleFor(x => x.ProductId).NotEmpty();

        RuleFor(x => x.Product.Name)
            .NotEmpty().MaximumLength(200);

        RuleFor(x => x.Product.Code)
            .NotEmpty().MaximumLength(50);

        RuleFor(x => x.Product.Price)
            .GreaterThan(0);

        RuleFor(x => x.Product.AvailableQuantity)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Product.CategoryId)
            .NotEmpty();
    }
}