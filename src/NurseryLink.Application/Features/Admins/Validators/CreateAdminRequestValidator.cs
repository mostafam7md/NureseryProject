using FluentValidation;
using NurseryLink.Application.Features.Admins.Dtos;
using NurseryLink.Domain.Constants;

namespace NurseryLink.Application.Features.Admins.Validators;

public sealed class CreateAdminRequestValidator : AbstractValidator<CreateAdminRequest>
{
    public CreateAdminRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(NurseryConstants.FullNameMaxLength);

        RuleFor(x => x.UserName)
            .NotEmpty()
            .Matches("^[a-zA-Z0-9_.-]{3,50}$")
            .WithMessage("Username must be 3-50 characters and contain only letters, digits, '.', '_' or '-'.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(NurseryConstants.EmailMaxLength);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.Privileges)
            .NotNull()
            .Must(p => p.All(v => Enum.IsDefined(v)))
            .WithMessage("Unknown privilege value supplied.");
    }
}
