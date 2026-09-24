using System;
using FluentValidation;

namespace ecommerce.Application.Inventory.Commands.Upload;

public class UploadCommandValidator : AbstractValidator<UploadCommand>
{
    public UploadCommandValidator()
    {
        RuleFor(x => x.Stream).NotEmpty().NotNull();
        RuleFor(x=> x.OriginalFileName).NotEmpty().NotNull();
    }
}
