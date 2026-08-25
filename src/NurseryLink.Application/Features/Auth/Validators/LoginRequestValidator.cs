using FluentValidation;
using NurseryLink.Application.Features.Auth.Dtos;
using NurseryLink.Domain.Constants;

namespace NurseryLink.Application.Features.Auth.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.UserNameOrEmail)
            .NotEmpty()
            .MaximumLength(NurseryConstants.EmailMaxLength);

        // Deliberately no complexity or minimum-length rules here. Password policy belongs on
        // create/change; enforcing it at login would lock out any account whose password predates
        // the current policy, and it leaks the policy to anonymous callers.
        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(128);
    }
}

public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .MaximumLength(200);
    }
}
