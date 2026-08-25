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
