using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Lms.Api.Auth;
using Lms.Application.Auth.Commands.ChangePassword;
using Lms.Application.Auth.Commands.Login;
using Lms.Application.Auth.Commands.Logout;
using Lms.Application.Auth.Commands.Refresh;
using Lms.Application.Auth.Dtos;
using Mediator;

namespace Lms.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Auth")
            .AddEndpointFilter<FetchMetadataEndpointFilter>();

        group.MapPost("/login", async (LoginRequest request, ISender sender, HttpContext http, CancellationToken ct) =>
        {
            var result = await sender.Send(new LoginCommand(request.Email, request.Password), ct);
            return WriteAuthResult(result, http);
        })
        .WithName("Login")
        .RequireRateLimiting("login")
        .Produces<AuthResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", async (ISender sender, HttpContext http, CancellationToken ct) =>
        {
            var result = await sender.Send(new RefreshTokenCommand(RefreshCookie.Read(http) ?? string.Empty), ct);
            return WriteAuthResult(result, http);
        })
        .WithName("Refresh")
        .Produces<AuthResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", async (ISender sender, HttpContext http, CancellationToken ct) =>
        {
            await sender.Send(new LogoutCommand(RefreshCookie.Read(http)), ct);
            RefreshCookie.Clear(http);
            return Results.NoContent();
        })
        .WithName("Logout");

        group.MapPost("/change-password", async (ChangePasswordRequest request, ISender sender, HttpContext http, CancellationToken ct) =>
        {
            var result = await sender.Send(new ChangePasswordCommand(request.CurrentPassword, request.NewPassword), ct);
            return WriteAuthResult(result, http);
        })
        .WithName("ChangePassword")
        .RequireAuthorization()
        .Produces<AuthResponse>()
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static Microsoft.AspNetCore.Http.IResult WriteAuthResult(Result<AuthTokens> result, HttpContext http)
    {
        if (result.IsSuccess)
        {
            var tokens = result.Value;
            RefreshCookie.Write(http, tokens.RefreshToken, tokens.RefreshTokenExpiresAt);
            return Results.Ok(new AuthResponse(tokens.AccessToken, tokens.AccessTokenExpiresAt, tokens.MustChangePassword));
        }

        if (result.Status == ResultStatus.Unauthorized)
        {
            var error = result.Errors.FirstOrDefault();
            var (type, detail) = error switch
            {
                LoginHandler.AccountLockedError =>
                    ("https://errors.vela.app/account-locked", "Your account is temporarily locked. Try again later."),
                ChangePasswordHandler.CurrentPasswordIncorrectError =>
                    ("https://errors.vela.app/invalid-credentials", "Current password is incorrect."),
                _ => ("https://errors.vela.app/invalid-credentials", "Incorrect email or password."),
            };

            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: detail, type: type);
        }

        // Validation failures throw ValidationException -> 422 (GlobalExceptionHandler) before reaching here.
        return result.ToMinimalApiResult();
    }
}

/// <summary>Login request body (API contract, distinct from the command).</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Change-password request body.</summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>Auth response body — access token + flags. The refresh token is set as an httpOnly cookie, never here.</summary>
public sealed record AuthResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, bool MustChangePassword);
