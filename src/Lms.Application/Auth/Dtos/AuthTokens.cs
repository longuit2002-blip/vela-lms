namespace Lms.Application.Auth.Dtos;

/// <summary>
/// Result of a successful login/refresh/change-password. The raw <see cref="RefreshToken"/> is
/// handed back to the API layer to set as an httpOnly cookie (never returned in the JSON body);
/// the access token and flags are returned to the client.
/// </summary>
public sealed record AuthTokens(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    bool MustChangePassword);
