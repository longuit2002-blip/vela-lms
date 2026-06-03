using Ardalis.Result;
using Lms.Application.Auth.Dtos;
using Mediator;

namespace Lms.Application.Auth.Commands.Login;

/// <summary>Authenticates with email + password, issuing an access token and a refresh-token family root.</summary>
public sealed record LoginCommand(string Email, string Password) : IRequest<Result<AuthTokens>>;
