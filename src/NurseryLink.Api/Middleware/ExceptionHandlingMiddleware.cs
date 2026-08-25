using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NurseryLink.Application.Common.Exceptions;

namespace NurseryLink.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    /// <summary>Non-standard but widely used code for "client hung up mid-request".</summary>
    private const int ClientClosedRequest = 499;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            await HandleExceptionAsync(context, exception);
        }
        catch (Exception exception)
        {
            // The response is already on the wire, so there is no status code left to set. Log and
            // rethrow rather than throwing a second exception from inside the handler.
            logger.LogError(exception, "Exception thrown after the response had started for {Method} {Path}.",
                context.Request.Method, context.Request.Path);
            throw;
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (status, title) = exception switch
        {
            AppValidationException => (StatusCodes.Status400BadRequest, "Validation failed."),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized."),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden."),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found."),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict."),
            OperationCanceledException when context.RequestAborted.IsCancellationRequested =>
                (ClientClosedRequest, "Client closed request."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}.",
                context.Request.Method, context.Request.Path);
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        if (exception is AppValidationException validation)
        {
            var problem = new ValidationProblemDetails(validation.Errors)
            {
                Title = title,
                Status = status,
                Instance = context.Request.Path
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
            return;
        }

        var details = new ProblemDetails
        {
            Title = title,
            Status = status,
            Instance = context.Request.Path,
            // Never echo an internal exception message to the caller; the log has the detail.
            Detail = status == StatusCodes.Status500InternalServerError ? null : exception.Message
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(details, JsonOptions));
    }
}
