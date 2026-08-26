using FluentValidation;
using NurseryLink.Application.Features.Classes.Dtos;
using NurseryLink.Domain.Constants;

namespace NurseryLink.Application.Features.Classes.Validators;

public sealed class CreateClassRequestValidator : AbstractValidator<CreateClassRequest>
{
    public CreateClassRequestValidator()
    {
        RuleFor(x => x.Name).ClassName();
    }
}

public sealed class RenameClassRequestValidator : AbstractValidator<RenameClassRequest>
{
    public RenameClassRequestValidator()
    {
        RuleFor(x => x.Name).ClassName();
    }
}

public sealed class AssignTeacherRequestValidator : AbstractValidator<AssignTeacherRequest>
{
    public AssignTeacherRequestValidator()
    {
        RuleFor(x => x.TeacherAccountId)
            .Must(id => id.Value != Guid.Empty)
            .WithMessage("A teacher must be supplied.");

        RuleFor(x => x.AssignmentType)
            .Must(Enum.IsDefined)
            .WithMessage("Unknown assignment type value supplied.");
    }
}

internal static class ClassValidationRules
{
    public static IRuleBuilderOptions<T, string> ClassName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MaximumLength(NurseryConstants.ClassNameMaxLength);
}
