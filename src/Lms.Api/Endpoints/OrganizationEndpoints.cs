using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Lms.Application.Organizations.Commands.CreateOrganization;
using Lms.Application.Organizations.Dtos;
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
        .WithName("ListOrganizations")
        .Produces<IReadOnlyList<OrganizationDto>>();

        group.MapPost("/", async (CreateOrganizationRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new CreateOrganizationCommand(request.Name, request.Slug), cancellationToken);

            // Validation failures throw ValidationException -> 422 (GlobalExceptionHandler), not here.
            // Handle Created/Conflict explicitly for a consistent contract: 201 carries a Location
            // header, and 409 is problem+json with a type URI (Ardalis maps both without these).
            if (result.Status == ResultStatus.Created)
                return Results.Created($"/api/v1/organizations/{result.Value.Id}", result.Value);

            if (result.Status == ResultStatus.Conflict)
                return Results.Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Conflict",
                    detail: result.Errors.FirstOrDefault(),
                    type: "https://errors.vela.app/conflict");

            return result.ToMinimalApiResult();
        })
        .WithName("CreateOrganization")
        .Produces<OrganizationDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }
}

/// <summary>Request body for creating an organization (API contract, kept distinct from the command).</summary>
public sealed record CreateOrganizationRequest(string Name, string Slug);
