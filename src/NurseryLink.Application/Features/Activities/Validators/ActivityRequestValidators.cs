using FluentValidation;
using NurseryLink.Application.Features.Activities.Dtos;
using NurseryLink.Domain.Constants;

namespace NurseryLink.Application.Features.Activities.Validators;

public sealed class LogMealRequestValidator : AbstractValidator<LogMealRequest>
{
    public LogMealRequestValidator()
    {
        RuleFor(x => x.MealType).Must(Enum.IsDefined).WithMessage("Unknown meal type.");
        RuleFor(x => x.Status).Must(Enum.IsDefined).WithMessage("Unknown eating status.");
    }
}

public sealed class LogToiletVisitRequestValidator : AbstractValidator<LogToiletVisitRequest>
{
    public LogToiletVisitRequestValidator()
    {
        RuleFor(x => x.VisitType).Must(Enum.IsDefined).WithMessage("Unknown toilet visit type.");
        RuleFor(x => x.Note!).MaximumLength(NurseryConstants.NoteMaxLength).When(x => x.Note is not null);
    }
}

public sealed class LogTemperatureRequestValidator : AbstractValidator<LogTemperatureRequest>
{
    public LogTemperatureRequestValidator()
    {
        // The plausible-range rule is NOT here. A reading of 55 C is a well-formed request that
        // states something impossible, which the plan answers with 422 rather than 400, so
        // ActivityLogService raises it instead. This validator only catches malformed input.
        RuleFor(x => x.Note!).MaximumLength(NurseryConstants.NoteMaxLength).When(x => x.Note is not null);
    }
}
