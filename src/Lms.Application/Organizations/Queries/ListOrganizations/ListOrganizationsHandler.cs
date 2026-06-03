using Ardalis.Result;
using Lms.Application.Abstractions;
using Lms.Application.Organizations.Dtos;
using Mediator;

namespace Lms.Application.Organizations.Queries.ListOrganizations;

public sealed class ListOrganizationsHandler(IOrganizationRepository repository)
    : IRequestHandler<ListOrganizationsQuery, Result<IReadOnlyList<OrganizationDto>>>
{
    public async ValueTask<Result<IReadOnlyList<OrganizationDto>>> Handle(
        ListOrganizationsQuery query,
        CancellationToken cancellationToken)
    {
        var organizations = await repository.ListAsync(cancellationToken);
        IReadOnlyList<OrganizationDto> dtos = organizations.Select(o => o.ToDto()).ToList();
        return Result.Success(dtos);
    }
}
