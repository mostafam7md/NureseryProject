using FluentValidation;
using NurseryLink.Application.Common.Validation;
using NurseryLink.Application.Features.Parents.Dtos;

namespace NurseryLink.Application.Features.Parents.Validators;

public sealed class CreateParentRequestValidator : AbstractValidator<CreateParentRequest>
{
    public CreateParentRequestValidator()
    {
        RuleFor(x => x.FullName).AccountFullName();
        RuleFor(x => x.UserName).AccountUserName();
        RuleFor(x => x.Email).AccountEmail();
        RuleFor(x => x.Password).AccountPassword();

        RuleFor(x => x.StudentIds)
            .NotNull()
            .Must(ids => ids.Count > 0)
            .WithMessage("A parent must be linked to at least one student.")
            .Must(ids => ids.All(id => id.Value != Guid.Empty))
            .WithMessage("Student ids must be supplied.");
    }
}
