using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Api;

/// <summary>
/// Catch-all exception handler. Validation failures (thrown by the Application's ValidationBehavior)
/// map to a 422 problem+json with field errors; everything else is a 500 (details only in Development).
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
                    Type = "https://errors.betterwork.vn/validation",
                },
            });
        }

        if (exception is BadHttpRequestException badRequest)
        {
            httpContext.Response.StatusCode = badRequest.StatusCode;
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = badRequest.StatusCode,
                    Title = "Bad request",
                    Detail = environment.IsDevelopment() ? badRequest.Message : "The request could not be processed.",
                },
            });
        }

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = environment.IsDevelopment() ? exception.ToString() : null,
            },
        });
    }
}
