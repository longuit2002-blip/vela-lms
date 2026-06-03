using FluentValidation;
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
        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
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
}
