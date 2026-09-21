using FluentValidation;

namespace ecommerce.Application.Categories.Commands.UpdateCategory;

public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().NotNull().WithMessage("Id cannot be empty");
        RuleFor(x => x.dto.Name).MinimumLength(4).NotEmpty().NotNull().WithMessage("Name cannot be empty");
    }
}