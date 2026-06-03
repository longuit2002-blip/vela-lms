using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Organizations.Dtos;
using Mediator;

namespace Lms.Application.Organizations.Queries.GetMyOrganization;

public sealed class GetMyOrganizationHandler(IOrganizationRepository repository, ITenantContext tenant)
    : IRequestHandler<GetMyOrganizationQuery, Result<OrganizationDto>>
{
    public async ValueTask<Result<OrganizationDto>> Handle(GetMyOrganizationQuery query, CancellationToken cancellationToken)
    {
        // Org id comes only from the authenticated tenant context — never from client input (no IDOR).
        if (tenant.OrganizationId == Guid.Empty)
            return Result.Unauthorized();

        var organization = await repository.FindByIdAsync(tenant.OrganizationId, cancellationToken);
        return organization is null
            ? Result.NotFound()
            : Result.Success(organization.ToDto());
    }
}
