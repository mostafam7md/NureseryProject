using FluentValidation;
using NurseryLink.Domain.Constants;

namespace NurseryLink.Application.Common.Validation;

/// <summary>
/// Field rules shared by every account-creating request. Admins, teachers and parents go through
/// the same login path, so their username shape and password policy must not drift apart — keeping
/// the rules here means tightening the policy once tightens it everywhere.
/// </summary>
public static class AccountValidationRules
{
    public const string UserNamePattern = "^[a-zA-Z0-9_.-]{3,50}$";

    public static IRuleBuilderOptions<T, string> AccountFullName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MaximumLength(NurseryConstants.FullNameMaxLength);

    public static IRuleBuilderOptions<T, string> AccountUserName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .Matches(UserNamePattern)
            .WithMessage("Username must be 3-50 characters and contain only letters, digits, '.', '_' or '-'.");

    public static IRuleBuilderOptions<T, string> AccountEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .EmailAddress()
            .MaximumLength(NurseryConstants.EmailMaxLength);

    public static IRuleBuilderOptions<T, string> AccountPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
}
