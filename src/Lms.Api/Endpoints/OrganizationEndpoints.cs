using Ardalis.Result.AspNetCore;
using Lms.Application.Organizations.Dtos;
using Lms.Application.Organizations.Queries.GetMyOrganization;
using Mediator;

namespace Lms.Api.Endpoints;

public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/organizations").WithTags("Organizations");

        // The walking skeleton's cross-tenant list + create are retired: a tenant user only ever sees
        // their own organization, and org creation now lives in the seed/platform path. The org id is
        // taken from the JWT (tenant context), never from client input.
        group.MapGet("/me", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetMyOrganizationQuery(), cancellationToken);
            return result.ToMinimalApiResult();
        })
        .WithName("GetMyOrganization")
        .RequireAuthorization()
        .Produces<OrganizationDto>()
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }
}
