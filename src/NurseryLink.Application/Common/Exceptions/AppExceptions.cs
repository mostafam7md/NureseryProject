namespace NurseryLink.Application.Common.Exceptions;

public sealed class UnauthorizedException(string message = "Invalid credentials.")
    : Exception(message);

public sealed class ForbiddenException(string message = "You do not have permission to perform this action.")
    : Exception(message);

public sealed class NotFoundException(string message)
    : Exception(message);

public sealed class ConflictException(string message)
    : Exception(message);

public sealed class AppValidationException(IDictionary<string, string[]> errors)
    : Exception("One or more validation errors occurred.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}

/// <summary>
/// The request was well-formed and the caller was allowed to make it, but a domain rule rejected
/// the value — a temperature outside the plausible human range, for example. Surfaces as 422,
/// which separates "you sent nonsense" (400) from "this value cannot be true" (422).
/// </summary>
public sealed class UnprocessableEntityException(string message)
    : Exception(message);
