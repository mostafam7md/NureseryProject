using FluentValidation;
using NurseryLink.Application.Common.Exceptions;

namespace NurseryLink.Application.Common.Extensions;

public static class ValidationExtensions
{
    /// <summary>Runs the validator and turns failures into an <see cref="AppValidationException"/>,
    /// which the API surfaces as a 400 ValidationProblemDetails.</summary>
    /// <remarks>Deliberately not named <c>ValidateAndThrowAsync</c>: FluentValidation ships an
    /// extension by that name on the same type, and having both in scope is an ambiguous call.</remarks>
    public static async Task ValidateOrThrowAsync<T>(
        this IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken = default)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (result.IsValid)
        {
            return;
        }

        throw new AppValidationException(result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
    }
}
