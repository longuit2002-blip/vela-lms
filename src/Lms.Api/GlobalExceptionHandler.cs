using FluentValidation;
using Lms.Application.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Api;

/// <summary>
/// Catch-all exception handler. Validation failures (thrown by the Application's ValidationBehavior)
/// map to a 422 problem+json with field errors; malformed requests to 400; everything else to 500
/// (details only in Development).
/// </summary>
public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, IHostEnvironment environment)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // First branch: authorization denials (RBAC behavior + ABAC handler guards both throw this).
        // Without it, denials would fall through to the 500 catch-all and leak detail in Development.
        if (exception is ForbiddenException forbidden)
        {
            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    // Reason only in Development — don't echo the permission taxonomy to clients in prod.
                    Detail = environment.IsDevelopment() ? forbidden.Message : null,
                    Type = "https://errors.vela.app/forbidden",
                },
            });
        }

        if (exception is ValidationException validationException)
        {
            // camelCase the field keys so they match the JSON property names clients see.
            var errors = validationException.Errors
                .GroupBy(e => ToCamelCase(e.PropertyName))
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ValidationProblemDetails(errors)
                {
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Title = "Validation failed",
                    Type = "https://errors.vela.app/validation",
                },
            });
        }

        if (exception is BadHttpRequestException badRequest)
        {
            return await WriteProblem(
                badRequest.StatusCode,
                "Bad request",
                environment.IsDevelopment() ? badRequest.Message : "The request could not be processed.");
        }

        return await WriteProblem(
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred.",
            environment.IsDevelopment() ? exception.ToString() : null);

        ValueTask<bool> WriteProblem(int status, string title, string? detail)
        {
            httpContext.Response.StatusCode = status;
            return problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail },
            });
        }
    }

    private static string ToCamelCase(string value) =>
        string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];
}
