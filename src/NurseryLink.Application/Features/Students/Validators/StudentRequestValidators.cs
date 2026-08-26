using FluentValidation;
using NurseryLink.Application.Features.Students.Dtos;
using NurseryLink.Domain.Constants;

namespace NurseryLink.Application.Features.Students.Validators;

public sealed class CreateStudentRequestValidator : AbstractValidator<CreateStudentRequest>
{
    public CreateStudentRequestValidator()
    {
        RuleFor(x => x.FullName).StudentFullName();
        RuleFor(x => x.DateOfBirth).PlausibleDateOfBirth();

        RuleFor(x => x.ClassId)
            .Must(id => id.Value != Guid.Empty)
            .WithMessage("A class must be supplied.");

        // Null means "allocate one for me"; an empty or blank string is a mistake, not a request.
        RuleFor(x => x.StudentCode!)
            .NotEmpty()
            .MaximumLength(NurseryConstants.StudentCodeMaxLength)
            .Matches("^[A-Za-z0-9-]+$")
            .WithMessage("Student code may contain only letters, digits and '-'.")
            .When(x => x.StudentCode is not null);
    }
}

public sealed class UpdateStudentRequestValidator : AbstractValidator<UpdateStudentRequest>
{
    public UpdateStudentRequestValidator()
    {
        RuleFor(x => x.FullName).StudentFullName();
        RuleFor(x => x.DateOfBirth).PlausibleDateOfBirth();
    }
}

public sealed class TransferStudentRequestValidator : AbstractValidator<TransferStudentRequest>
{
    public TransferStudentRequestValidator()
    {
        RuleFor(x => x.ClassId)
            .Must(id => id.Value != Guid.Empty)
            .WithMessage("A destination class must be supplied.");
    }
}

internal static class StudentValidationRules
{
    public static IRuleBuilderOptions<T, string> StudentFullName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(NurseryConstants.FullNameMaxLength);

    /// <summary>A nursery child is neither unborn nor an adult. Catches transposed or mistyped
    /// dates that would otherwise sit in the record unnoticed.</summary>
    public static IRuleBuilderOptions<T, DateOnly> PlausibleDateOfBirth<T>(this IRuleBuilder<T, DateOnly> rule) =>
        rule.Must(dob => dob <= DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("Date of birth cannot be in the future.")
            .Must(dob => dob >= DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-NurseryConstants.MaxStudentAgeYears))
                .WithMessage($"Date of birth implies an age over {NurseryConstants.MaxStudentAgeYears} years.");
}
