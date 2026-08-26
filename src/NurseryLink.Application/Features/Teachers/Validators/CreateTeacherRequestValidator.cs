using FluentValidation;
using NurseryLink.Application.Common.Validation;
using NurseryLink.Application.Features.Teachers.Dtos;

namespace NurseryLink.Application.Features.Teachers.Validators;

public sealed class CreateTeacherRequestValidator : AbstractValidator<CreateTeacherRequest>
{
    public CreateTeacherRequestValidator()
    {
        // A teacher carries no fields beyond the common account ones; the class assignment is
        // managed separately through the class endpoints.
        RuleFor(x => x.FullName).AccountFullName();
        RuleFor(x => x.UserName).AccountUserName();
        RuleFor(x => x.Email).AccountEmail();
        RuleFor(x => x.Password).AccountPassword();
    }
}
