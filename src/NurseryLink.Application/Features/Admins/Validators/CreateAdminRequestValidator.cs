using FluentValidation;
using NurseryLink.Application.Common.Validation;
using NurseryLink.Application.Features.Admins.Dtos;

namespace NurseryLink.Application.Features.Admins.Validators;

public sealed class CreateAdminRequestValidator : AbstractValidator<CreateAdminRequest>
{
    public CreateAdminRequestValidator()
    {
        RuleFor(x => x.FullName).AccountFullName();
        RuleFor(x => x.UserName).AccountUserName();
        RuleFor(x => x.Email).AccountEmail();
        RuleFor(x => x.Password).AccountPassword();

        RuleFor(x => x.Privileges)
            .NotNull()
            .Must(p => p.All(v => Enum.IsDefined(v)))
            .WithMessage("Unknown privilege value supplied.");
    }
}
