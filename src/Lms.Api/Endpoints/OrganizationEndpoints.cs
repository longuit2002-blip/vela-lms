using Ardalis.Result.AspNetCore;
using Lms.Application.Organizations.Commands.CreateOrganization;
using Lms.Application.Organizations.Queries.ListOrganizations;
using Mediator;

namespace Lms.Api.Endpoints;

public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/organizations").WithTags("Organizations");

        group.MapGet("/", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListOrganizationsQuery(), cancellationToken);
            return result.ToMinimalApiResult();
        })
        .WithName("ListOrganizations");

        group.MapPost("/", async (CreateOrganizationRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new CreateOrganizationCommand(request.Name, request.Slug), cancellationToken);
            // Validation failures throw ValidationException -> 422 (GlobalExceptionHandler), so
            // they never reach .ToMinimalApiResult() (which maps Result.Invalid -> 400).
            return result.ToMinimalApiResult();
        })
        .WithName("CreateOrganization");

        return app;
    }
}

/// <summary>Request body for creating an organization (API contract, kept distinct from the command).</summary>
public sealed record CreateOrganizationRequest(string Name, string Slug);
